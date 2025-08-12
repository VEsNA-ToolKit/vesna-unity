package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.Point3D;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import cartago.ObsProperty;
import com.fasterxml.jackson.core.type.TypeReference;
import org.json.JSONArray;
import org.json.JSONObject;

public class GrabbableArtifact extends AbstractMasElementArtifact {

    protected Point3D startingPosition;
    protected Point3D startingRotation;

    @OPERATION
    public void init(String artifactName, int webSocketPort, String transformJson) {
        super.init(artifactName, webSocketPort);
        defineObsProperty("isAvailable", true);
        defineObsProperty("currentOwner", "null");

        // parse the position from JSON
        JSONObject transformObject = new JSONObject(transformJson);
        JSONArray positionObject = transformObject.getJSONArray("position");
        JSONArray rotationObject = transformObject.getJSONArray("rotation");


        this.startingPosition = new Point3D(
            positionObject.getDouble(0),
            positionObject.getDouble(1),
            positionObject.getDouble(2)
        );

        this.startingRotation = new Point3D(
            rotationObject.getDouble(0),
            rotationObject.getDouble(1),
            rotationObject.getDouble(2)
        );

        defineObsProperty("position", this.startingPosition.toString());
        defineObsProperty("rotation", this.startingRotation.toString());
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
    void attemptRelease() {
        String agentName = getCurrentOpAgentId().getAgentName();
        if (agentName.equals(getOwner())) {
            updateObsProperty("isAvailable", true);
            updateObsProperty("currentOwner", "null");
            String position = getObsProperty("position").getValue().toString();
            String rotation = getObsProperty("rotation").getValue().toString();
            signal(getCurrentOpAgentId(), "released", this.artifactName, position, rotation);

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
            WsMessage wsMessage = ObjectMapperUtils.convertJsonStringToObject(message, new TypeReference<>() {});
            execInternalOp("signalAgentsByTick");
        } catch (Exception e){
            logger.info("Exception " + e);
        }
        finally {
            lock.unlock();
        }
    }

    // UTILITIES

    private void requestPositionFromUnity() {
        WsMessage wsMessage = new WsMessage();

        wsMessage.setMessageType("requestPosition");
        wsMessage.setMessagePayload("requestPositionFromUnity");
        wsMessage.setAgentName(this.artifactName);
        wsMessage.setParam(new JSONObject().put("artifactName", this.artifactName).toString());

        send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
    }
}
