package vesna;

import jason.asSemantics.*;
import jason.asSyntax.*;

import org.json.JSONObject;

public class stop extends DefaultInternalAction {

    @Override
    public Object execute( TransitionSystem ts, Unifier un, Term[] args ) throws Exception {

        JSONObject action = new JSONObject();
        action.put( "sender", ts.getAgArch().getAgName() );
        action.put( "receiver", "body" );
        action.put( "type", "start_conversation" );

        JSONObject data = new JSONObject();
        data.put( "with", args[0].toString() );

        action.put( "data", data)

        VesnaAgent ag = ( VesnaAgent ) ts.getAg();
        ag.perform( action.toString() );

        return true;
    }

}
