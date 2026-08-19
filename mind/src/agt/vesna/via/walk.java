package vesna;

import jason.asSemantics.*;
import jason.asSyntax.*;

import java.util.Set;

import org.json.JSONObject;
import static artifact.lib.utils.AgentUtils.cleanString;

public class walk extends DefaultInternalAction {

    // Types
    private static final String TYPE_STEP = "step";
    private static final String TYPE_GOTO = "goto";
    private static final String TYPE_NONE = "none";

    // walk()               performs a step
    // walk( n )            performs a step of length n
    // walk( target )       goes to target
    // walk( target, id )   goes to target with id

    @Override
    public Object execute( TransitionSystem ts, Unifier un, Term[] args ) throws Exception {
        String type = "none";

        if ( args.length == 0 )
            type = TYPE_STEP;
        else if ( args.length == 1 ){

            if ( args[0].isNumeric() )
                type = TYPE_STEP;
            else if (args[0].isLiteral() || args[0].isString())
                type = TYPE_GOTO;
       } else if ( args.length == 2 && (args[0].isLiteral() || args[0].isString()) && args[1].isNumeric() )
            type = TYPE_GOTO;
        else if ( args.length == 2 && (args[0].isLiteral() || args[0].isString()) && !args[1].isGround() )
            type = TYPE_GOTO;
        else
            return false;

        JSONObject data = new JSONObject();
        data.put( "type", type );
        if ( type.equals( "step" ) ){
            if ( args.length == 2 ){
                data.put( "length", ( ( NumberTerm ) args[1] ).solve() );
            }
        } else if ( type.equals( TYPE_GOTO ) ) {
            data.put( "target", cleanString(args[0].toString()));
            if ( args.length == 2 && args[1].isGround() )
                data.put( "id", ( ( NumberTerm ) args[1] ).solve() );
        }

        JSONObject action = new JSONObject();
        action.put( "sender", ts.getAgArch().getAgName() );
        action.put( "receiver", "body" );
        action.put( "type", "walk" );
        action.put( "data", data );

        System.out.println( action.toString() );

        VesnaAgent ag = ( VesnaAgent ) ts.getAg();
        ag.perform( action.toString() );

        return true;
    }

}
