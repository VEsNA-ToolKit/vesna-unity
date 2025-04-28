package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.INTERNAL_OPERATION;
import cartago.ObsProperty;
import com.fasterxml.jackson.core.type.TypeReference;

public class CounterArtifact extends AbstractMasElementArtifact {

    @Override
    protected void init(String artifactName, int webSocketPort) {
        super.init(artifactName, webSocketPort);
        // Define count property initialized to 0
        defineObsProperty("count", 0);
    }

    @INTERNAL_OPERATION
    private void increment() {
        writeLog("Incrementing the counter...");
        // Access the property
        ObsProperty prop = getObsProperty("count");
        // Update the property value
        prop.updateValue(prop.intValue() + 1);
    }

    @INTERNAL_OPERATION
    private int getCounter() {
        // Access the property
        ObsProperty prop = getObsProperty("count");
        // Update the property value
        return prop.intValue();
    }

    @Override
    public void onMessageReceived(String message) {
        writeLog("Message received from Unity:" + message);
        try {
            lock.lock();
            WsMessage wsMessage = ObjectMapperUtils.convertJsonStringToObject(message, new TypeReference<>() {});
            execInternalOp("increment");
            writeLog("Actual counter: " + getCounter());
            // Add to the agent the number
            execInternalOp("signalAgent", wsMessage.getAgentName(), wsMessage.getAgentEvent(),
                    getCounter());
        } catch (Exception e) {
            logger.info("Exception " + e);
        }finally {
            lock.unlock();
        }
    }
}
