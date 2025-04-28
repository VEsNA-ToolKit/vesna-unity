package artifact.lib.maselements;

import artifact.lib.model.ArtifactInfo;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.AgentUtils;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.INTERNAL_OPERATION;
import cartago.OPERATION;
import cartago.OpFeedbackParam;
import com.fasterxml.jackson.core.type.TypeReference;
import jason.asSyntax.ASSyntax;
import jason.asSyntax.Atom;
import jason.asSyntax.Literal;
import jason.asSyntax.parser.ParseException;

import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.*;
import java.util.stream.Collectors;

import static artifact.lib.utils.AgentUtils.*;

public abstract class AbstractMasElementAgent extends AbstractMasElement {

    // e.g. bobAgent
    protected String agentNameForJava;
    // e.g. bob
    protected String agentNameForJason;

    protected void init(String agentName, int port) {
        jacamoElementName = agentName;
        this.agentNameForJava = agentName;
        this.agentNameForJason = agentName.split("Agent")[0];
        initializeLogger(agentName);
        initializeWebSocketConnection(port, agentNameForJava);
        // Clean file by removing old logs
        cleanLogFile();
        // writeLog("WebSocket server for - " + agentName +
        //         " started on port: " + server.getPort());
    }

    @OPERATION
    public void notifyUnity(String request) {
        // Parse the literal string
        Literal literal = Literal.parseLiteral(request);
        Map<String, String> msg = new HashMap<>();
        msg.put("agentName", literal.getTerm(0).toString());
        msg.put("messageType", AgentUtils.cleanString(literal.getTerm(1).toString()));
        msg.put("messagePayload", AgentUtils.cleanString(literal.getTerm(2).toString()));
        // Prepare the message
        WsMessage wsMessage = new WsMessage();
        wsMessage.setMessageType(msg.get("messageType"));
        wsMessage.setMessagePayload(msg.get("messagePayload"));
        wsMessage.setAgentName(msg.get("agentName"));
        // Stringify into JSON object
        String msgToSendJson = ObjectMapperUtils.convertIntoJsonString(wsMessage);

        writeLog(msgToSendJson);

        send(msgToSendJson);
    }

    @Override
    public void onMessageReceived(String message) {
        writeLog("Message received from Unity: " + message);
        try {
            WsMessage wsMessage = ObjectMapperUtils.convertJsonStringToObject(message, new TypeReference<>() {
            });
            evaluateReceivedMessage(wsMessage);
        } catch (Exception e) {
            logger.info("Exception encountered.");
            e.printStackTrace();
        }
    }

    private void evaluateReceivedMessage(WsMessage wsMessage) throws Exception {
        writeLog("Message: " + wsMessage.toString());
        String messageType = wsMessage.getMessageType();
        switch (messageType) {
            case "destinationReached" -> { // Need to signal the agent
                // Evaluate the messageType to signal
                String destReached = (String) wsMessage.getParam();
                execInternalOp("signalAgent", agentNameForJason, wsMessage.getAgentEvent(), destReached);
            }
            case "eyes" -> { // Avatar has seen something (another avatar or an artifact)
                if ("agentSeen".equals(wsMessage.getAgentEvent())) {
                    writeLog("Agent has seen an agent!!");
                    String seenElement = (String) wsMessage.getParam();
                    execInternalOp("signalAgent", agentNameForJason, wsMessage.getAgentEvent(), seenElement);
                } else if ("artifactSeen".equals(wsMessage.getAgentEvent())) { // Agent has seen an artifact
                    ArtifactInfo artifactInfo = ObjectMapperUtils
                            .convertObject(wsMessage.getParam(), ArtifactInfo.class);
                    writeLog("Agent seen an artifact!!");
                    writeLog(artifactInfo.toString());
                    execInternalOp("signalAgent", agentNameForJason, wsMessage.getAgentEvent(),
                            convertObjectToLiteral(artifactInfo));
                }
            }
            default -> throw new Exception();
        }
    }

    @Override
    @INTERNAL_OPERATION
    public void onConnectionEstablished() {
        writeLog("Connection established. Signal agent.");
        execInternalOp("signalAgent", agentNameForJason, "connection_established", true);
    }

    // Method called by the agent .asl file to save log on monitoring.log
    @OPERATION
    public void writeLogForAgent(Object[] params) {
        return;
        // StringBuilder message = new StringBuilder();
        // for (Object param : params) {
        //     message.append(param.toString()).append(" ");
        // }
        // writeLog(message.toString());
    }

    // Method to update agents' beliefs by memorizing what they see in the scene
    @OPERATION
    public void updateSeenElements(Object[] seenElements, String elementToAdd, OpFeedbackParam<Literal> updatedSeenElements) throws ParseException {
        Literal literal = updateAgentMemory(seenElements, elementToAdd);
        // Set the output parameter
        updatedSeenElements.set(literal);
    }

    @OPERATION
    public void checkIfAgentIsAFriend(String agentName, Object[] friendList, OpFeedbackParam<Literal> isAFriend) {
        List<String> friends = mapJacamoListIntoStringList(friendList);

        if (friends.contains(agentName)) {
            isAFriend.set(Literal.parseLiteral("true"));
        } else {
            isAFriend.set(Literal.parseLiteral("false"));
        }
    }

    // Method to process the conversation between two agents
    @OPERATION
    public abstract void processConversation(String content, OpFeedbackParam<String> replyContent);

}
