package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import com.fasterxml.jackson.core.type.TypeReference;

public class DoorArtifact extends AbstractMasElementArtifact {

    private String propertyName = "isDoorOpen";
    @OPERATION
    void init(String artifactName, int webSocketPort, String property) {
        super.init(artifactName, webSocketPort);
        initializeProperty(propertyName, Boolean.parseBoolean(property));
    }

    @Override
    public void onMessageReceived(String message) {
        try{
            writeLog("Message received from Unity: " + message);
            WsMessage wsMessage = ObjectMapperUtils.convertJsonStringToObject(message, new TypeReference<>() {});
            setProperty(propertyName, (Boolean) wsMessage.getParam());
            execInternalOp("signalAgentsByTick");
        } catch (Exception e){
            logger.info("Exception " + e);
        }
    }
}
