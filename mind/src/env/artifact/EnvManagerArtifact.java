package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import com.fasterxml.jackson.core.type.TypeReference;

import java.util.List;

public class EnvManagerArtifact extends AbstractMasElementArtifact {

    @OPERATION
    public void retrieveArtifactsByType(String artifactType, String agentName){
        try{
            lock.lock();
            WsMessage wsMessage = prepareMessage("retrieveArtifacts", "all_artifact_by_type", agentName, artifactType);
            writeLog("Sending message: " + ObjectMapperUtils.convertIntoJsonString(wsMessage));
            send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
        }finally {
            lock.unlock();
        }
    }

    @OPERATION
    public void retrieveAllArtifacts(String agentName){
        try{
            lock.lock();
            WsMessage wsMessage = prepareMessage("retrieveArtifacts", "all_artifact", agentName, null);
            send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
        }finally {
            lock.unlock();
        }
    }

    @OPERATION
    public void retrieveNearestArtifactByType(String artifactType, String agentName){
        try{
            lock.lock();
            WsMessage wsMessage = prepareMessage("retrieveArtifacts", "nearest", agentName, artifactType);
            writeLog("Sending message: " + ObjectMapperUtils.convertIntoJsonString(wsMessage));
            send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
        }finally {
            lock.unlock();
        }
    }

    private static WsMessage prepareMessage(String messageType, String messagePayload, String agentName, String param) {
        WsMessage wsMessage = new WsMessage();
        wsMessage.setMessageType(messageType);
        wsMessage.setMessagePayload(messagePayload);
        wsMessage.setAgentName(agentName);
        wsMessage.setParam(param);
        return wsMessage;
    }

    @Override
    public void onConnectionEstablished(){
        super.onConnectionEstablished();
        execInternalOp("signalAllAgents", "connection_env_leader_established", true);
    }

    @Override
    public void onMessageReceived(String message) {
        writeLog("Message received from Unity: " + message);
        try{
            lock.lock();
            // Signal the agent that requested the artifact
            WsMessage wsMessage = ObjectMapperUtils
                    .convertJsonStringToObject(message, new TypeReference<>() {});
            List<String> artifactNames = ObjectMapperUtils.convertObject(wsMessage.getParam(), new TypeReference<>() {});

            execInternalOp("signalAgent", wsMessage.getAgentName(),
                    wsMessage.getAgentEvent(), artifactNames.toArray());
        }catch (Exception e){
            e.printStackTrace();
        }finally {
            lock.unlock();
        }
    }
}
