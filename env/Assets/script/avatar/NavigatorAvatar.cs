using UnityEngine;

// ============================================================
// NavigatorAvatar.cs  ->  : VesnaAvatarBase
// ============================================================
// Agente che NON esplora: fa tutto quello che faceva prima ma
// niente esplorazione. Riceve il percorso dall'envManager (via
// reach_dest nodo per nodo) e cammina.
//
// Sul GameObject tieni:
//   - TopologicalMovement
//   - NavMeshAgent
//
// NON mettere: ShopperAvatarScript, AutonomousWalking,
//              BeliefTransmitter (non esplora, non trasmette grafo).
// ============================================================

public class NavigatorAvatar : VesnaAvatarBase
{
    protected override void Awake()
    {
        agentFile = string.IsNullOrEmpty(agentFile) ? "vesna.asl" : agentFile;
        JaCaMoAgentClassPath = "artifact.lib.maselements.AgentMasElement";

        // jacamoReceiver default = "vesna_agent" (va bene per vesna.asl)

        base.Awake();
    }
}
