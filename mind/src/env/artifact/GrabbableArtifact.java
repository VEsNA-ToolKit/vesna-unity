package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.Point3D;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import cartago.ObsProperty;
import com.fasterxml.jackson.core.type.TypeReference;
import org.json.JSONObject;

public class GrabbableArtifact extends AbstractMasElementArtifact {


    @OPERATION
    public void init(String artifactName, int webSocketPort) {
        super.init(artifactName, webSocketPort);
        defineObsProperty("isAvailable", true);
        defineObsProperty("currentOwner", "null");

        // Request the grabbable status from Unity
        requestGrabbableStatus();
    }

    /**
     * Grab operation, it must first check if the artifact is available or not.
     * The artifact might be unavailable because if:
     * - It's being currently held by another agent
     * - It's not available to the current agent
     */
    @OPERATION
    void attemptGrab() {
        String agentName = getCurrentOpAgentId().getAgentName();
        writeLog("Agent is grabbing " + this.artifactName);
        if (isAvailable()) {
            updateObsProperty("isAvailable", false);
            updateObsProperty("currentOwner", agentName);
            signal(getCurrentOpAgentId(), "grabbed", this.artifactName);

            writeLog(String.format("Agent %s grabbed the artifact", agentName));
        } else {
            // Signal failure
            System.out.println("Grab failed - artifact not available");
            failed("grabbed", "artifact_not_available");
        }
    }

    @OPERATION
    void attemptRelease(String snapName) {
        String agentName = getCurrentOpAgentId().getAgentName();
        if (agentName.equals(getOwner())) {
            updateObsProperty("isAvailable", true);
            updateObsProperty("currentOwner", "null");
            signal(getCurrentOpAgentId(), "released", this.artifactName, snapName);

            writeLog(String.format("Agent %s released the artifact", agentName));
        }
        else {
            writeLog(String.format("Agent %s attempted to release the artifact but is not the owner", agentName));
            failed("release", "not_owner");
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
            lock.lock();
            writeLog("[GrabbableArtifact] Message received from Unity: " + message);

            JSONObject messageJson = new JSONObject(message);

            if (messageJson.has("type") && messageJson.getString("type").equals("grabbable_status")) {
                updateObsProperty("isAvailable", messageJson.getBoolean("data"));
            }

            execInternalOp("signalAgentsByTick");
        } catch (Exception e){
            logger.info("Exception " + e);
        }
        finally {
            lock.unlock();
        }
    }

    private void requestGrabbableStatus() {
        WsMessage wsMessage = new WsMessage();
        wsMessage.setMessageType("grabbableStatus");
        wsMessage.setMessagePayload("is_grabbable");
        wsMessage.setAgentName(this.artifactName);

        send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
    }
}
