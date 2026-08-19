using System.Collections.Generic;
using UnityEngine;

// ============================================================
// AgentJacamoBridge.cs
// ============================================================
// Componente comune ai DUE agenti (esploratore e navigatore).
// Si occupa SOLO di notificare a JaCaMo, sul canale dell'avatar:
//   - la stanza corrente  (current_room)  -> chiamato da LocalizationTest
//   - l'arrivo a un nodo   (reached_destination) -> chiamato da TopologicalMovement
//
// NON ha niente a che fare col grafo: quello resta nel BeliefTransmitter.
// Manda tutto via avatar.SendMessageToJaCaMoBrain, cioè sul canale a cui
// è collegato il cervello DI QUESTO agente. Per questo il receiver è null:
// il messaggio viaggia sul canale proprio dell'agente e arriva al suo cervello.
//
// Va su ENTRAMBI i prefab, insieme all'avatar (AgentAvatar) e al NavMeshAgent.
// ============================================================

public class AgentJacamoBridge : MonoBehaviour
{
    [Tooltip("Nome dell'agente Jason di QUESTO body (come nel .jcm / nel log della MAS Console). " +
             "Es: \"vesna_agent\" per il navigatore, \"explorer\" per l'esploratore.")]
    public string jacamoReceiver = "vesna_agent";

    private AgentAvatar avatar;

    private string _lastArrivedNode = null;
    public string LastArrivedNode => _lastArrivedNode;

    void Awake()
    {
        avatar = GetComponent<AgentAvatar>();
        if (avatar == null)
            Debug.LogError("[AgentJacamoBridge] Nessun AgentAvatar sul GameObject.");
    }

    // Arrivo a un nodo -> reached(place, Nodo) lato Jason
    public void SendMovementCompleted(string targetNode)
    {
        _lastArrivedNode = targetNode;

        avatar?.SendMessageToJaCaMoBrain(
            UnityJacamoIntegrationUtil.CreateAndConvertJacamoMessageIntoJsonString(
                "destinationReached", null, "reached_destination", null, targetNode));

        SendCurrentRoom(targetNode);
    }

    // Stanza corrente -> current_room(Stanza) lato Jason
    public void SendCurrentRoom(string roomId)
    {
        string toSend = _lastArrivedNode ?? roomId;

        var msg = new BeliefMessage
        {
            beliefType = "current_room",
            payload = new Dictionary<string, object> { { "roomId", toSend } }
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
        string fullMsg = UnityJacamoIntegrationUtil.CreateAndConvertJacamoMessageIntoJsonString(
            msg.beliefType, null, null, null, json);

        avatar?.SendMessageToJaCaMoBrain(fullMsg);
        Debug.Log($"[AgentJacamoBridge] current_room inviata: {toSend}");
    }
        // Arrivo a un OGGETTO: reached(place, Label) SENZA toccare current_room.
    public void SendObjectReached(string label)
    {
        avatar?.SendMessageToJaCaMoBrain(
            UnityJacamoIntegrationUtil.CreateAndConvertJacamoMessageIntoJsonString(
                "destinationReached", null, "reached_destination", null, label));
    }

    [System.Serializable]
    private class BeliefMessage
    {
        public string beliefType;
        public Dictionary<string, object> payload;
    }
}