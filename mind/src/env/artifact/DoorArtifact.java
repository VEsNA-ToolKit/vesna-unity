package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.core.type.TypeReference;
import java.util.LinkedHashMap;
import java.util.Map;

// ============================================================
// DoorArtifact.java
// ============================================================
// Artefatto JaCaMo per una PORTA FISICA nella scena.
//
// PROPRIETÀ FISICHE OSSERVABILI:
//   isOpen   : bool   — porta fisicamente aperta
//   isLocked : bool   — sbarrata, non apribile
//   type     : String — "door"
//   x,y,z    : float  — posizione mondo (navigazione)
//
// Niente di algoritmico: niente explored, niente orderFW/BW.
// Queste informazioni vivono nel TopologicalGraph Unity-side
// e nelle credenze Jason dopo la trasmissione.
//
// CICLO DI VITA:
//   1. Creato nel .jcm all'avvio (uno per ogni porta nella scena)
//   2. Durante l'esplorazione Unity aggiorna isOpen/isLocked
//      ogni volta che la porta cambia stato fisicamente
//   3. Dopo l'esplorazione l'agente Jason usa tryOpen() per
//      attraversare fisicamente la porta durante la pianificazione
// ============================================================
public class DoorArtifact extends AbstractMasElementArtifact {

    @OPERATION
    void init(String id, int port,
              String isOpen, String isLocked,
              float x, float y, float z) {

        super.init(id, port);
        defineObsProperty("isOpen",   Boolean.parseBoolean(isOpen));
        defineObsProperty("isLocked", Boolean.parseBoolean(isLocked));
        defineObsProperty("type",     "door");
        defineObsProperty("x", x);
        defineObsProperty("y", y);
        defineObsProperty("z", z);
    }

   @Override
    public void onMessageReceived(String message) {
        try {
            WsMessage wsMsg = ObjectMapperUtils.convertJsonStringToObject(
                    message, new TypeReference<>() {});

            if ("update_status".equals(wsMsg.getMessageType())) {
                // param è una Map con isOpen e isLocked
                java.util.Map<String, Object> params = 
                    (java.util.Map<String, Object>) wsMsg.getParam();
                if (params != null) {
                    Object isOpen   = params.get("isOpen");
                    Object isLocked = params.get("isLocked");
                    if (isOpen   != null) getObsProperty("isOpen").updateValue(isOpen);
                    if (isLocked != null) getObsProperty("isLocked").updateValue(isLocked);
                    writeLog("[" + artifactName + "] isOpen=" + isOpen + " isLocked=" + isLocked);
                }
            }
            execInternalOp("signalAgentsByTick");
        } catch (Exception e) {
            logger.info("DoorArtifact error: " + e);
        }
    }

    // Agente chiede di aprire fisicamente la porta.
    // Unity risponde con update_status.
    @OPERATION
    void tryOpen() {
        try {
            Map<String, Object> payload = new LinkedHashMap<>();
            payload.put("operation",    "open_door");
            payload.put("artifactName", artifactName);
            send(new ObjectMapper().writeValueAsString(payload));
        } catch (Exception e) {
            writeLog("Errore tryOpen: " + e.getMessage());
        }
    }
}