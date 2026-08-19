using UnityEngine;

// ============================================================
// VerticalConnectorScript.cs
// ============================================================
// Trigger fisico per scale o ascensori nella scena Unity.
// Va messo sul GameObject della scala/ascensore.
//
// Quando l'agente entra nel trigger:
//   → notifica ExplorationManager via evento statico
//   → ExplorationManager decide se usarlo (solo se il piano
//     corrente è completamente esplorato)
//
// SETUP NELL'INSPECTOR:
//   - connectorId : deve matchare l'id nel BuildingGraph
//   - connectorType: Stairs o Elevator
//   - targetFloor : piano di destinazione
//
// LAYER: "connector" — visibile al VisionCone
// ============================================================

public class VerticalConnectorScript : MonoBehaviour
{
    [Header("Configurazione")]
    public GameObject connectorIdObject;
    public string connectorId => connectorIdObject != null ? connectorIdObject.name : string.Empty;// MESSO COSì PERCHè SE NO DOVEVAMO CAMBIARE TUTTO IL CODICE 
    public VerticalConnector.ConnectorType connectorType;
    public int targetFloor;
    

    // Evento statico: ExplorationManager si abbona
    public static event System.Action<string, int, Vector3> OnConnectorReached;

    private bool triggered = false;

    void Awake()
    {
        var col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.radius    = 1.0f;
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Agent")) return;
        triggered = true;

        var manager = other.GetComponent<ExplorationManager>() 
                ?? FindObjectOfType<ExplorationManager>();
        if (manager == null) { Debug.LogWarning("[Connector] ExplorationManager non trovato!"); return; }

        var connector = manager.buildingGraph?.connectors.Find(c => c.id == connectorId);
        if (connector == null) { Debug.LogWarning($"[Connector] '{connectorId}' non trovato!"); return; }

        int currentFloor = manager.currentFloor;
        int resolvedTarget;

        if (connector.floorFrom == currentFloor)
            resolvedTarget = connector.floorTo;
        else if (connector.bidirectional && connector.floorTo == currentFloor)
            resolvedTarget = connector.floorFrom;
        else
        {
            Debug.LogWarning($"[Connector] '{connectorId}' non applicabile dal piano {currentFloor}");
            triggered = false;
            return;
        }

        OnConnectorReached?.Invoke(connectorId, resolvedTarget, transform.position);
        Debug.Log($"[Connector] {connectorId} raggiunto → piano {resolvedTarget}");
    }

    public void ResetTrigger() => triggered = false;
}