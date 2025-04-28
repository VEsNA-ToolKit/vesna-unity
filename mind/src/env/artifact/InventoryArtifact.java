package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;

public class InventoryArtifact extends AbstractMasElementArtifact {

    void init(String artifactName, int webSocketPort, String properties) {
        // Initialize websocket connection and log
        super.init(artifactName, webSocketPort);
    }
}
