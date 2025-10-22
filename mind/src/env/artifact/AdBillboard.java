package vesna;
import cartago.Artifact;
import cartago.OPERATION;
import cartago.OpFeedbackParam;
import jason.asSyntax.ListTerm;
import jason.asSyntax.ListTermImpl;

import static jason.asSyntax.ASSyntax.*;

import java.util.Collection;
import java.util.ArrayList;

public class AdBillboard extends Artifact {

    // private Collection<String> agents = new ArrayList<>();
    private ListTerm agents = new ListTermImpl();

    public void init() {
        defineObsProperty("adBillboard", agents);
    }

    @OPERATION
    public void look_at( ) {
        String ag_name = getCurrentOpAgentId().getAgentName();
        agents.add( createLiteral( ag_name ) );
        getObsProperty( "adBillboard" ).updateValue( agents );
    }

    @OPERATION
    public void stop_look_at( ) {
        String ag_name = getCurrentOpAgentId().getAgentName();
        agents.remove( createLiteral( ag_name ) );
        getObsProperty( "adBillboard" ).updateValue( agents );
    }

}