using UnityEngine;

// ============================================================
// ExplorerAvatar.cs  ->  : VesnaAvatarBase
// ============================================================
// Agente ESPLORATORE: fa tutto quello che faceva prima (occhi,
// voce, cono visivo, ecc.) MENO il giro shopping, esplora come
// adesso E sa muoversi nodo per nodo.
//
// Sul GameObject tieni anche:
//   - i tuoi componenti di esplorazione (ExplorationManager,
//     VisionCone, RoomObject..., LocalizationTest, ecc.)
//   - BeliefTransmitter        (gli serve per TRASMETTERE il grafo)
//   - TopologicalMovement
//   - NavMeshAgent
//
// NON mettere: ShopperAvatarScript, AvatarScript, AutonomousWalking.
// ============================================================

public class ExplorerAvatar : VesnaAvatarBase
{
    protected override void Awake()
    {
        // Imposta l'asl dell'esploratore (se non già impostato nell'Inspector)
        agentFile = string.IsNullOrEmpty(agentFile) ? "explorer_v1.asl" : agentFile;
        JaCaMoAgentClassPath = "artifact.lib.maselements.AgentMasElement";

        // ATTENZIONE: imposta jacamoReceiver (campo nell'Inspector) col nome
        // dell'agente esploratore lato Jason, NON "vesna_agent".

        base.Awake();
    }
}
