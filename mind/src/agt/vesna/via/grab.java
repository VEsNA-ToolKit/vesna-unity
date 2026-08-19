package vesna;

import cartago.ArtifactId;
import jason.asSemantics.DefaultInternalAction;
import jason.asSemantics.TransitionSystem;
import jason.asSemantics.Unifier;
import jason.asSyntax.Atom;
import jason.asSyntax.ObjectTerm;
import jason.asSyntax.Term;
import jason.infra.local.LocalAgArch;
import org.json.JSONObject;

public class grab extends DefaultInternalAction {

    @Override
    public Object execute(TransitionSystem ts, Unifier un, Term[] args) throws Exception {
        if (args.length != 1) {
            return false; // Invalid arguments
        }

        JSONObject data = new JSONObject();
        data.put("name", args[0].toString());

        JSONObject action = new JSONObject();
        action.put( "sender", ts.getAgArch().getAgName() );
        action.put( "receiver", "body" );
        action.put( "type", "grab" );
        action.put( "data", data );

        VesnaAgent ag = ( VesnaAgent ) ts.getAg();
        ag.perform(action.toString());

        return true;
    }
}
