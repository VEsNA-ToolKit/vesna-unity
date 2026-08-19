using UnityEngine;

// ============================================================
// CorridorPoleScript.cs
// ============================================================
// Rappresenta UN ESTREMO fisico del corridoio.
// Due per corridoio: Pole_1 e Pole_2, figli di CorridorData.
//
// COSA FA ORA (dopo il cambio architettura):
// È un dato fisico puro. Non comunica con JaCaMo direttamente.
// Notifica ExplorationManager quando l'agente lo tocca tramite
// evento C# statico — niente WebSocket durante l'esplorazione.
//
// ExplorationManager si abbona a OnPoleTouched e reagisce:
//   - Primo polo → avvia transito A→B, attiva DoorSensor
//   - Secondo polo → calcola orderBW, avvia ispezione porte
//
// LAYER: "corridor" — visibile al VisionCone dell'agente
// ============================================================

public class CorridorPoleScript : MonoBehaviour
{
    // Assegnato automaticamente da CorridorData.ConfigureChildren()
    [HideInInspector] public string corridorId;
    [HideInInspector] public string poleId; // "1" o "2"

    // Evento statico: qualunque polo toccato notifica ExplorationManager
    // Parametri: corridorId, poleId, posizione mondo
    public static event System.Action<string, string, Vector3> OnPoleTouched;

    private bool triggered = false;

    void Awake()
    {
        // Aggiunge SphereCollider trigger se non esiste
        var col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.radius    = 0.4f;
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Agent")) return;
        triggered = true;
        OnPoleTouched?.Invoke(corridorId, poleId, transform.position);
        Debug.Log($"[Pole] {corridorId} polo {poleId} toccato a {transform.position}");
    }

    // Chiamato da ExplorationManager per backtracking
    public void ResetTrigger() => triggered = false;
}
