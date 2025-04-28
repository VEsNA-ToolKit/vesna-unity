package artifact.lib.maselements;

import artifact.lib.websocket.*;
import cartago.AgentId;
import cartago.Artifact;
import cartago.INTERNAL_OPERATION;

import java.io.FileWriter;
import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;
import java.util.logging.Logger;
import java.net.URI;

public abstract class AbstractMasElement extends Artifact{

    protected Logger logger = null;
    // protected WebSocketChannel server;
    private WsClient client;
    protected String jacamoElementName;
    protected final Lock lock = new ReentrantLock();

    public abstract void onMessageReceived(String message);

    public abstract void onConnectionEstablished();

    protected void initializeLogger(String loggerClassName) {
        this.logger = Logger.getLogger(loggerClassName);
    }

    protected void initializeWebSocketConnection(int port, String name) {
        String host = "localhost";
        try {
            client = new WsClient( new URI( "ws://" + host + ":" + port ) );
            client.setMsgHandler( this::onMessageReceived );
        } catch ( Exception e ) {
            e.printStackTrace();
        }
        // InetSocketAddress address = new InetSocketAddress(host, port);
        // server = new WebSocketChannel(address);
        // server.setJacamoElement(this);
        // server.setJacamoElementName(name);
        // server.start();
    }

    public void send( String msg ) {
        client.send( msg );
    }

    // Signal specific agent
    @INTERNAL_OPERATION
    protected void signalAgent(String agentName, String plan, Object param) {
        AgentId agentId = new AgentId(agentName, agentName, 0, "", null);
        if(param == null){
            signal(agentId, plan);
        } else {
            signal(agentId, plan, param);
        }
    }

    protected void cleanLogFile() {
        String filePath = jacamoElementName + "-monitoring.log";

        Path logFilePath = Paths.get("./logs", filePath);
        // Use FileWriter in append mode
        try (FileWriter fw = new FileWriter(logFilePath.toFile(), false)) {
            fw.write("");
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    protected void writeLog(String message) {
        String filePath = jacamoElementName + "-monitoring.log";

        Path logFilePath = Paths.get("./logs", filePath);
        // Create the directory if it doesn't exist
        try {
            Files.createDirectories(logFilePath.getParent());
        } catch (IOException e) {
            e.printStackTrace();
        }

        // Get current timestamp
        LocalDateTime now = LocalDateTime.now();
        DateTimeFormatter formatter = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");
        String timestamp = now.format(formatter);

        // Use FileWriter in append mode
        try (FileWriter fw = new FileWriter(logFilePath.toFile(), true)) { // 'true' for append mode
            fw.write("[" + timestamp + "]" + " - " + jacamoElementName + " - " + message);
            fw.write("\n");
        } catch (IOException e) {
            e.printStackTrace();
        }

        logger.info(message);
    }

}
