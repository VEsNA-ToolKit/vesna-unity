package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import com.fasterxml.jackson.core.type.TypeReference;

/**
 * RoomObjectArtifact.java
 *
 * Artefatto CArtAgO che rappresenta un oggetto fisico
 * scoperto a runtime dal cono visivo dell'agente Unity.
 * Creato dinamicamente dall'agente Jason quando riceve
 * la belief new_object(...).
 *
 * OBSERVABLE PROPERTIES:
 *   artifactId  — ID univoco dell'oggetto (es. "chair_lobby")
 *   roomId      — stanza in cui si trova l'oggetto
 *   type        — costante "room_object"
 *   x, y, z     — posizione 3D nella scena Unity
 *
 * CICLO DI VITA:
 *   1. Unity vede l'oggetto → invia new_object(Id, RoomId, ...) all'agente
 *   2. Agente crea l'artefatto con makeArtifact
 *   3. Agente fa focus → ottiene le observable properties come beliefs
 *   4. Se l'oggetto cambia stanza → Unity invia update_room →
 *      roomId viene aggiornato automaticamente
 */
public class RoomObjectArtifact extends AbstractMasElementArtifact {

    @OPERATION
    void init(String id, int port, String roomId,
              double x, double y, double z) {

        super.init(id, port);

        defineObsProperty("artifactId", id);
        defineObsProperty("roomId",     roomId);
        defineObsProperty("type",       "room_object");
        defineObsProperty("x",          x);
        defineObsProperty("y",          y);
        defineObsProperty("z",          z);

        log("RoomObjectArtifact '" + id + "' creato in stanza '" + roomId + "'");
    }

    // -----------------------------------------------------------------------
    // Ricezione messaggi da Unity
    // -----------------------------------------------------------------------

    @Override
    public void onMessageReceived(String message) {
        try {
            WsMessage wsMsg = ObjectMapperUtils.convertJsonStringToObject(
                    message, new TypeReference<>() {});

            // Aggiornamento stanza se l'oggetto viene visto in una stanza diversa
            if ("update_room".equals(wsMsg.getMessageType())) {
                java.util.Map<String, Object> params =
                    (java.util.Map<String, Object>) wsMsg.getParam();
                if (params != null) {
                    Object newRoomId = params.get("roomId");
                    if (newRoomId != null) {
                        getObsProperty("roomId").updateValue(newRoomId.toString());
                        log("'" + artifactName + "' aggiornato: ora in stanza '" + newRoomId + "'");
                    }
                }
            }

            execInternalOp("signalAgentsByTick");
        } catch (Exception e) {
            logger.info("RoomObjectArtifact error: " + e);
        }
    }
}
