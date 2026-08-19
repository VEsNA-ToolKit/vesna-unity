package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import com.fasterxml.jackson.core.type.TypeReference;

public class GenericArtifact extends AbstractMasElementArtifact {
    @OPERATION
    void init(String artifactName, int webSocketPort, String property) {
        super.init(artifactName, webSocketPort);
    }
}
