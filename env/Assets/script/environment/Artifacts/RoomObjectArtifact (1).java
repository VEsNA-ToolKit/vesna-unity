package artifacts;

import cartago.*;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

/**
 * RoomObjectArtifact.java
 *
 * Artefatto CArtAgO che rappresenta un oggetto fisico scoperto a runtime.
 * NON viene pre-dichiarato nel .jcm: viene creato dinamicamente dall'agente
 * Jason la prima volta che il cono visivo vede l'oggetto in Unity.
 *
 * Proprietà osservabili (beliefs nell'agente dopo il focus):
 *   roomId(RoomId)      → la stanza in cui si trova l'oggetto
 *   type("room_object") → costante per distinguerlo da porte/varchi
 *
 * Ciclo di vita:
 *   1. Unity vede l'oggetto → invia new_object(Id, RoomId) all'agente
 *   2. Agente crea l'artefatto con: makeArtifact(Id, "artifacts.RoomObjectArtifact", ...)
 *   3. Agente fa focus → ottiene in_room(Id, RoomId) nel belief base
 *   4. Se Unity vede l'oggetto in stanza diversa → invia update_room → belief aggiornata
 */
public class RoomObjectArtifact extends Artifact {

    private String artifactId;
    private int wsPort;

    // -----------------------------------------------------------------------
    // Inizializzazione — chiamata da makeArtifact nel piano Jason
    // -----------------------------------------------------------------------

    /**
     * @param id     ID univoco dell'oggetto (es. "chair_lobby")
     * @param port   Porta WebSocket su cui ascolta questo artefatto
     * @param roomId Stanza in cui è stato visto per la prima volta
     */
    void init(String id, int port, String roomId) {
        this.artifactId = id;
        this.wsPort = port;

        defineObsProperty("artifactId", id);
        defineObsProperty("roomId",     roomId);
        defineObsProperty("type",       "room_object");

        startWebSocketListener(port);

        log("RoomObjectArtifact '" + id + "' creato in stanza '" + roomId + "'");
    }

    // -----------------------------------------------------------------------
    // Ricezione messaggi da Unity (RoomObjectBridge.cs)
    // -----------------------------------------------------------------------

    /**
     * Gestisce i messaggi JSON da Unity.
     *
     * Messaggio atteso per aggiornamento stanza:
     *   { "operation": "update_room", "artifactName": "chair_lobby", "roomId": "room_102" }
     *
     * Il bridge C# invia questo messaggio SOLO se la stanza è cambiata
     * rispetto all'ultimo invio, quindi qui aggiorniamo sempre.
     */
    @INTERNAL_OPERATION
    void onMessageReceived(String message) {
        try {
            JsonObject json = JsonParser.parseString(message).getAsJsonObject();
            String operation = json.get("operation").getAsString();

            if ("update_room".equals(operation)) {
                String newRoomId = json.get("roomId").getAsString();
                updateObsProperty("roomId", newRoomId);
                log("'" + artifactId + "' aggiornato: ora in stanza '" + newRoomId + "'");
            }

        } catch (Exception e) {
            log("Errore parsing messaggio: " + e.getMessage());
        }
    }

    // -----------------------------------------------------------------------
    // Helper WebSocket — stessa infrastruttura di DoorArtifact.java
    // -----------------------------------------------------------------------

    private void startWebSocketListener(int port) {
        // Inserire qui la stessa logica WebSocket già usata in DoorArtifact.java.
        // Il listener deve chiamare:
        //   execInternalOp("onMessageReceived", rawMessage)
        // ogni volta che arriva un messaggio da Unity.
    }
}
