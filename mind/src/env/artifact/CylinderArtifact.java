package artifact;

import cartago.OPERATION;

public class CylinderArtifact extends GrabbableArtifact {
    @OPERATION
    public void init(String artifactName, int webSocketPort) {
        super.init(artifactName, webSocketPort);
    }
}
