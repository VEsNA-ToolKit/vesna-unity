package artifact.lib.websocket;

// Websockets

import java.net.InetSocketAddress;
import java.util.concurrent.CompletableFuture;

import artifact.lib.maselements.AbstractMasElement;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import lombok.Getter;
import lombok.Setter;
import org.java_websocket.WebSocket;
import org.java_websocket.handshake.ClientHandshake;
import org.java_websocket.server.WebSocketServer;

public class WebSocketChannel extends WebSocketServer {

    private WebSocket webSocketClient;
    @Setter
    // Could be an artifact in the environment or the agent
    private AbstractMasElement jacamoElement;
    @Getter
    @Setter
    // Artifact name or agent name
    private String jacamoElementName;

    public WebSocketChannel(InetSocketAddress address) {
        super(address);
    }

    @Override // Open connection and send welcome message to the clients that are listening on the channel
    public void onOpen(WebSocket conn, ClientHandshake handshake) {
        WsMessage wsMessage = new WsMessage("wsInitialization",
                "Connection opened for " + jacamoElementName + " - jacamo side!",
                null, null, null);
        // Save connection info
        webSocketClient = conn;
        sendMessage(ObjectMapperUtils.convertIntoJsonString(wsMessage));
        // Notify observer
        jacamoElement.onConnectionEstablished();
    }

    @Override
    public void onClose(WebSocket conn, int code, String reason, boolean remote) {
        System.out.println("closed " + conn.getRemoteSocketAddress() + " with exit code " + code + " additional info " + reason);
    }

    @Override // Received message from the client (Unity)
    public void onMessage(WebSocket conn, String message) {
        jacamoElement.onMessageReceived(message);
    }

    @Override
    public void onError(WebSocket conn, Exception ex) {
        System.err.println("an error occurred on connection " + conn.getRemoteSocketAddress() + ":" + ex);
    }

    @Override
    public void onStart() {
        System.out.println("WebSocket channel started successfully");
    }


    public void sendMessage(String msg) {
        System.out.println("Sending message - " + msg);
        // Send message in async
        CompletableFuture.runAsync(() -> {
            boolean isMsgSent = false;
            while (!isMsgSent) {
                try {
                    if(webSocketClient!= null){
                        webSocketClient.send(msg);
                    }
                    isMsgSent = true;
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }


}
