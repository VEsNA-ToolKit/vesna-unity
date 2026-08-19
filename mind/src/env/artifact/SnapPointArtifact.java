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

public class SnapPointArtifact extends AbstractMasElementArtifact {

    @OPERATION
    public void init(String artifactName, int webSocketPort, String transformJson) {
        super.init(artifactName, webSocketPort);
        defineObsProperty("isAvailable", true);
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
}
