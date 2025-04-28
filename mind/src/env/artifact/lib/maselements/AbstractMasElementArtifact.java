package artifact.lib.maselements;

import artifact.lib.utils.ObjectMapperUtils;
import cartago.INTERNAL_OPERATION;
import cartago.OPERATION;
import cartago.ObsProperty;
import com.fasterxml.jackson.core.type.TypeReference;

import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

public abstract class AbstractMasElementArtifact extends AbstractMasElement {
    protected String artifactName;

    protected void init(String artifactName, int port) {
        jacamoElementName = artifactName;
        this.artifactName = artifactName;
        initializeLogger(artifactName);
        initializeWebSocketConnection(port, artifactName);
        // Clean file by removing old logs
        cleanLogFile();
        // writeLog("WebSocket server for - " + artifactName + " started on port: " + server.getPort());
    }

    // Method to initialize artifact property
    protected <T> void initializeProperty(String propertyName, T property) {
        defineObsProperty(propertyName, property);
    }

    @INTERNAL_OPERATION
    protected <T> void setProperty(String propertyName, T property) {
        // Update the property value
        getObsProperty(propertyName).updateValue(ObjectMapperUtils.convertObject(property, new TypeReference<T>() {}));
    }

    @INTERNAL_OPERATION
    protected <R> R getProperty(String propertyName, TypeReference<R> target) {
        // Retrive the property
        ObsProperty prop = getObsProperty(propertyName);
        // Retrieve the value and convert it to List<FruitInfo>
        Object value = prop.getValue();
        return ObjectMapperUtils.convertObject(value, target);
    }

    // Signal all agents by passing a parameter
    @INTERNAL_OPERATION
    protected void signalAllAgents(String plan, Object object) {
        lock.lock();
        try { // Signal all the agents
            signal(plan, object);
        } finally {
            lock.unlock();
        }
    }

    // Just signal tick message to let agents perceive environment changes
    @INTERNAL_OPERATION
    protected void signalAgentsByTick() {
        lock.lock();
        try { // Signal all the agents
            signal("tick");
        } finally {
            lock.unlock();
        }
    }

    @Override
    public void onMessageReceived(String message) {}

    @Override
    public void onConnectionEstablished(){
        writeLog("Connection Established");
    }
}
