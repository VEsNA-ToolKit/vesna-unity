package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import cartago.ObsProperty;
import com.fasterxml.jackson.core.type.TypeReference;

public class GrabbableArtifact extends AbstractMasElementArtifact {

    void init(String artifactName, int webSocketPort, String properties) {
        super.init(artifactName, webSocketPort);
        defineObsProperty("isAvailable", true);
        defineObsProperty("currentOwner", "null");
    }

    /**
     * Grab operation, it must first check if the artifact is available or not.
     * The artifact might be unavailable because if:
     * - It's being currently held by another agent
     * - It's not available to the current agent
     * @param agentName - the agent that tried to grab the object
     */
    @OPERATION
    void grab(String agentName) {
        if (isAvailable()) {
            defineObsProperty("isAvailable", false);
            defineObsProperty("currentOwner", agentName);

            writeLog(String.format("Agent %s grabbed the artifact", agentName));
        }
    }

    @OPERATION
    void release(String agentName) {
        if (getOwner() == agentName) {
            defineObsProperty("isAvailable", true);
            defineObsProperty("currentOwner", "null");

            writeLog(String.format("Agent %s released the artifact", agentName));
        }
    }

    @OPERATION
    String getOwner() {
        ObsProperty prop = getObsProperty("currentOwner");
        return prop.getValue().toString();
    }

    @OPERATION
    boolean isAvailable() {
        ObsProperty prop = getObsProperty("isAvailable");
        return prop.booleanValue();
    }

    @Override
    public void onMessageReceived(String message) {
        try {
            writeLog("[GrabbableArtifact] Message received from Unity: " + message);
            execInternalOp("signalAgentsByTick");
        } catch (Exception e){
            logger.info("Exception " + e);
        }
    }

}
