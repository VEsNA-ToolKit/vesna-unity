package vesna;

import jason.asSemantics.*;
import jason.asSyntax.*;

import static jason.asSyntax.ASSyntax.*;

import java.net.URI;

import org.json.JSONObject;
import org.json.JSONArray;
import jason.asSyntax.parser.ParseException;

// VesnaAgent class extends the Agent class making the agent embodied;
// It connects to the body using a WebSocket connection;
// It needs two beliefs: address( ADDRESS ) and port( PORT ) that describe the address and port of the WebSocket server;
// In order to use it you should add to your .jcm:
// > agent alice:alice.asl {
// >      beliefs: address( localhost )
// >               port( 8080 )
// >      ag-class: vesna.VesnaAgent
// > }

public class VesnaAgent extends Agent{

    private WsClient client;
    private String my_name;

    // Override loadInitialAS method to connect to the WebSocket server (body)
    @Override
    public void loadInitialAS( String asSrc ) throws Exception {

        super.loadInitialAS( asSrc );
        my_name = getTS().getAgArch().getAgName();

        // Get the address from beliefs
        Unifier address_unifier = new Unifier();
        believes( parseLiteral( "address( Address )" ), address_unifier );

        // Get the port from beliefs
        Unifier port_unifier = new Unifier();
        believes( parseLiteral( "port( Port )" ), port_unifier );

        // Check if the address and port beliefs are defined
        if ( address_unifier.get( "Address" ) == null || port_unifier.get( "Port" ) == null ) {
                stop( "address and port beliefs are not defined!" );
                return;
        }

        // Store address and port in variables and initialize the WebSocket client
        String address = address_unifier.get( "Address" ).toString();
        int port = ( int ) ( ( NumberTerm ) port_unifier.get( "Port" ) ).solve();

        System.out.printf( "[%s] Body is at %s:%d%n", my_name, address, port );

        URI body_address = new URI( "ws://" + address + ":" + port );
        client = new WsClient( body_address );

        // Connect the two handle functions to the client object
        client.setMsgHandler( new WsClientMsgHandler() {
            @Override
            public void handle_msg( String msg ) {
                vesna_handle_msg( msg );
            }

            @Override
            public void handle_error( Exception ex ) {
                vesna_handle_error( ex );
            }
        }  );
        // Connect the body
        client.connect();
    }

    // perform sends an action to the body
    public void perform( String action ) {
        System.out.println( "[LOG] " + action );
        client.send( action );
    }

    // sense signals the mind about a perception
    private void sense( Literal perception ) {
        try {
            Message signal = new Message( "signal", my_name, my_name , perception );
            getTS().getAgArch().sendMsg( signal );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
    }

    // handle_event takes all the data from an event and senses a perception
    private void handle_event( JSONObject event ) {
        String event_type = event.getString( "type" );
        String event_status = event.getString( "status" );
        String event_reason = event.getString( "reason" );
        Literal perception = createLiteral( event_type, createLiteral( event_status ), createLiteral( event_reason ) );
        sense( perception );
    }

    // handle_sight takes all the data from a sight and adds a belief
    private void handle_sight( JSONObject sight ) {
        String type = sight.getString( "type" );
        String model = "";
        if ( ! sight.isNull( "model" ) )
            model = sight.getString( "model" );
        String name = sight.getString( "name" );
        System.out.println( "Got type: " + type + ", model: " + model + ", name: " + name );
        try {
            Literal percept = createSightPercept(type, model, name);
            sense( percept );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
    }

    private void handle_movement( JSONObject data ) {
        String type = data.getString( "type" );
        String target = data.getString( "name" ); 
        try {
            Literal percept = parseLiteral( "reached(" + type + ", " + target + ")" );
            sense( percept );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
    }

    private void handle_door( JSONObject data ) {
        try {
            if ( data.getBoolean( "status" ) )
                addBel( parseLiteral( "door_open" ) );
            else
                delBel( parseLiteral( "door_open" ) );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
    }

    private void handle_arts( JSONObject data ) {
        JSONArray art_names = data.getJSONArray( "names" );
        try {
            Literal percept = parseLiteral( "art_names(" + art_names.toString() + ")");
            sense( percept );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
    }

    // this function handles incoming messages from the body
    // available types are: signal, sight
    public void vesna_handle_msg( String msg ) {
        System.out.println( "Received message: " + msg );
        JSONObject log = new JSONObject( msg );
        String sender = log.getString( "sender" );
        String receiver = log.getString( "receiver" );
        String type = log.getString( "type" );
        JSONObject data = log.getJSONObject( "data" );
        switch( type ){
            case "signal" -> handle_event( data );
            case "sight" -> handle_sight( data );
            case "movement" -> handle_movement( data );
            case "door" -> handle_door( data );
            case "artifactStrategy" -> handle_arts( data );
            default -> System.out.println( "Unknown message type: " + type );
        }
    }

    // Stops the agent: prints a message and kills the agent
    private void stop( String reason ) {
        System.out.println( "[" + my_name + " ERROR] " + reason );
        kill_agent();
    }

    // Handles a connection error: prints a message and kills the agent
    public void vesna_handle_error( Exception ex ){
        System.out.println( "[" + my_name + " ERROR] " + ex.getMessage() );
        kill_agent();
    }

    // Kills the agent calling the internal actions to drop all desires, intentions and events and then kill the agent;
    // This is necessary to avoid the agent to keep running after the kill_agent call ( that otherwise is simply enqueued ).
    private void kill_agent() {
        System.out.println( "[" + my_name + " ERROR] Killing agent" );
        try {
            InternalAction drop_all_desires = getIA( ".drop_all_desires" );
            InternalAction drop_all_intentions = getIA( ".drop_all_intentions" );
            InternalAction drop_all_events = getIA( ".drop_all_events" );
            InternalAction action = getIA( ".kill_agent" );

            drop_all_desires.execute( getTS(), new Unifier(), new Term[] {} );
            drop_all_intentions.execute( getTS(), new Unifier(), new Term[] {} );
            drop_all_events.execute( getTS(), new Unifier(), new Term[] {} );
            action.execute( getTS(), new Unifier(), new Term[] { createString( my_name ) } );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
    }

    // Helper method to create sight percepts
    private Literal createSightPercept(String type, String model, String name) throws ParseException {
        if (model.isEmpty()) {
            return parseLiteral(String.format("seen(\"%s\", _, \"%s\")", type, name));
        } else {
            return parseLiteral(String.format("seen(\"%s\", \"%s\", \"%s\")", type, model, name));
        }
    }
}
