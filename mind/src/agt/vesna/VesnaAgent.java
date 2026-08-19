package vesna;

import jason.asSemantics.*;
import jason.asSyntax.*;

import static jason.asSyntax.ASSyntax.*;

import java.net.URI;

import org.apache.commons.text.StringEscapeUtils;
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
        // In loadInitialAS, sostituisci client.connect() con:
        boolean connected = false;
        for (int i = 0; i < 10; i++) {
            try {
                client.connect();
                connected = true;
                break;
            } catch (Exception e) {
                System.out.printf("[%s] Tentativo %d fallito, ritento...%n", my_name, i+1);
                Thread.sleep(1000);
            }
        }
        if (!connected) {
            stop("impossibile connettersi dopo 10 tentativi");
            return;
        }
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
        name = StringEscapeUtils.escapeJava(name);
        System.out.println( "Got type: " + type + ", model: " + model + ", name: " + name );
        try {
            Literal percept = createSightPercept(type, model, name);
            sense( percept );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
    }

   private void handle_movement( JSONObject data ) {
    try {
        String name = data.getString("name");
        Literal percept = parseLiteral(
            String.format("reached(place, \"%s\")", name)
        );
        sense( percept );
        System.out.println("[VesnaAgent] reached(place, " + name + ") segnalato.");
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
        String type = log.getString( "type" );

       JSONObject data;
        Object rawData = log.isNull("data") ? null : log.get("data");
        if (rawData == null) {
            if (!type.equals("error")) {
                System.out.println("[VesnaAgent] Messaggio '" + type + "' senza data, ignorato.");
            }
            return;
        }
        if (rawData instanceof String) {
            data = new JSONObject((String) rawData);
        } else {
            data = log.getJSONObject("data");
        }
        switch( type ){
            case "signal" -> handle_event( data );
            case "sight" -> handle_sight( data );
            case "movement" -> handle_movement( data );
            case "door" -> handle_door( data );
            case "artifactStrategy" -> handle_arts( data );
            case "node"                 -> handle_node(data);
            case "edge"                 -> handle_edge(data);
            case "connector_link"       -> handle_connector_link(data);
            case "door_dist"            -> handle_door_dist(data);
            case "new_object"           -> handle_new_object(data);
            case "corridor"             -> handle_corridor(data);
            case "exploration_complete" -> handle_exploration_complete(data);
            case "current_room" -> handle_current_room(data);
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
    // Invece di kill_agent(), riprova la connessione
    try {
        Thread.sleep(2000);
        System.out.println( "[" + my_name + "] Riprovo connessione..." );
        client.reconnect();
    } catch (Exception e) {
        System.out.println( "[" + my_name + " ERROR] Retry fallito, killing agent" );
        kill_agent();
    }
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
            return parseLiteral(String.format("seen(%s, _, \"%s\")", type, name));
        } else {
            String test = String.format("seen(%s, %s, \"%s\")", type, model, name);
            return parseLiteral(test);
        }
    }
    private void handle_node(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String lit = String.format(
            "node(\"%s\", \"%s\", %d, %s, %s, %s, \"%s\", \"%s\", %d)",
            p.getString("id"),
            p.getString("type"),
            p.getInt("floor"),
            p.get("x"), p.get("y"), p.get("z"),
            p.optString("corridorId", ""),
            p.optString("poleLabel", ""),
            p.optInt("wsPort", 0)
        );
        addBel(parseLiteral(lit));
    } catch (Exception e) { e.printStackTrace(); }
}

private void handle_edge(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String edgeId = p.getString("id");
        String goName = edgeId;
        String direction = "none";
        if      (edgeId.startsWith("fw_"))      { goName = edgeId.substring(3); direction = "fw"; }
        else if (edgeId.startsWith("bw_"))      { goName = edgeId.substring(3); direction = "bw"; }
        else if (edgeId.startsWith("seg_"))     { goName = edgeId.substring(4); direction = "seg"; }
        else if (edgeId.startsWith("central_")) { goName = edgeId.substring(8); direction = "central"; }

        String lit = String.format(
            "edge(\"%s\", \"%s\", \"%s\", %d, \"%s\", \"%s\", \"%s\", \"%s\", %s, %s, %d, %s)",
            goName,
            p.getString("from"),
            p.getString("to"),
            p.getInt("floor"),
            p.optString("edgeType", ""),
            p.optString("doorState", ""),
            p.optString("side", ""),
            direction,
            p.optDouble("distFromA", 0.0),
            p.optDouble("distFromB", 0.0),
            p.optInt("wsPort", 0),
            p.optBoolean("isPhysical", false)
        );
        addBel(parseLiteral(lit));
    } catch (Exception e) { e.printStackTrace(); }
}

private void handle_connector_link(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String lit = String.format(
            "connector_link(\"%s\", %d, %d, %s, %s, %s, %s, %s, %s, %s)",
            p.getString("id"),
            p.getInt("floorA"),
            p.getInt("floorB"),
            p.get("bidirectional"),
            p.get("posAx"), p.get("posAy"), p.get("posAz"),
            p.get("posBx"), p.get("posBy"), p.get("posBz")
        );
        addBel(parseLiteral(lit));
    } catch (Exception e) { e.printStackTrace(); }
}

private void handle_door_dist(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String lit = String.format(
            "door_dist(\"%s\", \"%s\", %d, %s)",
            p.getString("roomA"),
            p.getString("roomB"),
            p.getInt("floor"),
            p.get("distance")
        );
        addBel(parseLiteral(lit));
    } catch (Exception e) { e.printStackTrace(); }
}

private void handle_new_object(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String lit = String.format(
            "new_object(\"%s\", \"%s\", \"%s\", %d, %s, %s, %s)",
            p.getString("artifactId"),
            p.getString("roomId"),
            p.getString("artifactType"),
            p.getInt("wsPort"),
            p.get("x"), p.get("y"), p.get("z")
        );
        addBel(parseLiteral(lit));
    } catch (Exception e) { e.printStackTrace(); }
}

private void handle_corridor(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String lit = String.format(
            "corridor(\"%s\", \"%s\", \"%s\", %d, %d)",
            p.getString("corridorId"),
            p.getString("poleA"),
            p.getString("poleB"),
            p.getInt("floor"),
            p.optInt("wsPort", 0)
        );
        addBel(parseLiteral(lit));
    } catch (Exception e) { e.printStackTrace(); }
}

private void handle_exploration_complete(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String lit = String.format(
            "exploration_complete(\"%s\", \"%s\", %d, %d, %d, %d)",
            p.getString("buildingId"),
            p.getString("capturedAt"),
            p.getInt("totalFloors"),
            p.getInt("totalNodes"),
            p.getInt("totalEdges"),
            p.getInt("totalDists")
        );
        addBel(parseLiteral(lit));
    } catch (Exception e) { e.printStackTrace(); }
}

private void handle_current_room(JSONObject data) {
    try {
        JSONObject p = data.getJSONObject("payload");
        String roomId = p.getString("roomId");
        
        // Rimuovi la vecchia belief e aggiungi la nuova
        try { delBel(parseLiteral("current_room(_)")); } 
        catch (Exception ignored) {}
        
        addBel(parseLiteral(String.format("current_room(\"%s\")", roomId)));
        System.out.println("[VesnaAgent] Stanza corrente: " + roomId);
    } catch (Exception e) { e.printStackTrace(); }
}
}