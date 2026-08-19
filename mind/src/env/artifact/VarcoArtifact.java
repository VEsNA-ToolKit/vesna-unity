package artifact;

import artifact.lib.maselements.AbstractMasElementArtifact;
import cartago.OPERATION;

// ============================================================
// VarcoArtifact.java
// ============================================================
// Artefatto JaCaMo per un VARCO APERTO (apertura senza porta).
//
// DIFFERENZA DA DoorArtifact:
//   - isOpen  è sempre true  — non cambia mai
//   - isLocked è sempre false — non può essere bloccato
//   - Nessuna azione tryOpen — non serve, è sempre attraversabile
//   - type = "varco" — l'agente non tenta open_door
//
// Esempi di varco: ingresso sala riunioni senza porta,
// apertura tra due corridoi, soglia senza battente.
//
// PROPRIETÀ FISICHE OSSERVABILI:
//   isOpen   : true  — fisso
//   isLocked : false — fisso
//   type     : "varco"
//   x,y,z    : float — posizione mondo
// ============================================================
public class VarcoArtifact extends AbstractMasElementArtifact {

    @OPERATION
    void init(String id, int port, float x, float y, float z) {
        super.init(id, port);
        defineObsProperty("isOpen",   true);
        defineObsProperty("isLocked", false);
        defineObsProperty("type",     "varco");
        defineObsProperty("x", x);
        defineObsProperty("y", y);
        defineObsProperty("z", z);
    }

    // VarcoArtifact non riceve messaggi da Unity —
    // il suo stato fisico non cambia mai.
    // onMessageReceived ereditato da AbstractMasElementArtifact
    // gestisce la connessione WebSocket base.
}