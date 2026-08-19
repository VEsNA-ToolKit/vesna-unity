package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import com.fasterxml.jackson.core.type.TypeReference;

public class CorridorArtifact extends AbstractMasElementArtifact {

    @OPERATION
    void init(String id, int port,
            double p1x, double p1y, double p1z,
            double p2x, double p2y, double p2z) {
        super.init(id, port);
        defineObsProperty("corridorId",       id);
        defineObsProperty("pole1x",           p1x);
        defineObsProperty("pole1y",           p1y);
        defineObsProperty("pole1z",           p1z);
        defineObsProperty("pole2x",           p2x);
        defineObsProperty("pole2y",           p2y);
        defineObsProperty("pole2z",           p2z);
        defineObsProperty("polesInitialized", true);
    }

    @Override
    public void onMessageReceived(String message) {
        try {
            WsMessage wsMsg = ObjectMapperUtils.convertJsonStringToObject(
                    message, new TypeReference<>() {});

            // Gestione futura — es. "set_occupied", "set_closed", ecc.
            // switch (wsMsg.getOperation()) {
            //     case "set_occupied" -> ...
            // }

            execInternalOp("signalAgentsByTick");
        } catch (Exception e) {
            logger.info("CorridorArtifact error: " + e);
        }
    }
}