using UnityEngine;
using UnityEngine.AI;

// ============================================================
// DoorVarcoScript.cs
// ============================================================
// Script UNICO per porte fisiche e varchi aperti.
//
// AGGIORNAMENTO — gestione lato porta:
// Una porta ha DUE facce. roomNameFront è il luogo sul lato
// dove punta la normale (transform.forward) della porta.
// roomNameBack è il lato opposto.
//
// CONVENZIONE nell'Inspector:
//   Orienta il GameObject porta in modo che transform.forward
//   punti verso la stanza (non il corridoio).
//   Front = stanza, Back = corridoio/esterno.
//
//   Esempio: porta tra corridoio_1 e stanza_101
//     roomNameFront = "stanza_101"   (forward punta in stanza)
//     roomNameBack  = "corridoio_1"  (back è il corridoio)
//
// GetRoomNameFromAgentPosition(agentPos):
//   Restituisce il roomName del luogo OLTRE la porta rispetto
//   a dove si trova l'agente. ExplorationManager la chiama
//   prima di EnterThroughEdge() per sapere quale nodo creare.
// ============================================================

public class DoorVarcoScript : MonoBehaviour
{
    public enum ElementType { Door, Varco }

    [Header("Tipo fisico")]
    public ElementType elementType = ElementType.Door;

    [Header("Stato fisico")]
    public bool isOpen   = false;
    public bool isLocked = false;

    public bool isCorridorLink = false;

    // ---> AGGIUNGI QUESTA RIGA <---
    [Header("Modello 3D (Da disattivare all'apertura)")]
    public GameObject physicalModel;

    [Header("Stanze collegate — trascina i GameObject dall'Inspector")]
    [Tooltip("GameObject della stanza/corridoio sul lato FRONT (dove punta transform.forward).\n" +
             "Orienta la porta in modo che forward punti verso la stanza.\n" +
             "Il nome del GameObject viene usato come ID nodo nel grafo.")]
    public GameObject roomFront;

    [Tooltip("GameObject della stanza/corridoio sul lato BACK (opposto al forward).\n" +
             "Tipicamente il corridoio o l'ambiente esterno.\n" +
             "Il nome del GameObject viene usato come ID nodo nel grafo.")]
    public GameObject roomBack;

    // ── Nomi derivati dai GameObject — usati dal grafo ───────────────────────
    // Non scrivere mai a mano: trascina roomFront/roomBack nell'Inspector.
    public string roomNameFront => roomFront != null ? roomFront.name : gameObject.name + "_front";
    public string roomNameBack  => roomBack  != null ? roomBack.name  : gameObject.name + "_back";

    // --------------------------------------------------------
    // PROPRIETÀ DERIVATE
    // --------------------------------------------------------
    public bool IsTraversable =>
        elementType == ElementType.Varco || (isOpen && !isLocked);

    public bool IsBlocked =>
        elementType != ElementType.Varco && isLocked;

    // --------------------------------------------------------
    // DATI GEOMETRICI — calcolati da ExplorationManager a runtime
    // --------------------------------------------------------
    [HideInInspector] public float  distFromA  = -1f;
    [HideInInspector] public float  distFromB  = -1f;
    [HideInInspector] public string side       = "";
    [HideInInspector] public int    orderFW    = -1;
    [HideInInspector] public bool   discovered = false;

    private static readonly string[] corridorKeywords = {
    "corridoio", "corridor", "disimpegno", "ingresso", "entrance", "entry",
    "atrio", "hall", "hallway", "lobby", "pianerottolo", "landing",
    "ballatoio", "galleria", "passaggio", "passage"
    };

    private bool IsCorridorLike(string roomName)
    {
        string lower = roomName.ToLower();
        foreach (var keyword in corridorKeywords)
            if (lower.Contains(keyword)) return true;
        return false;
    }

    // --------------------------------------------------------
    // METODO CHIAVE: roomName del luogo OLTRE la porta
    //
    // dot = dot(transform.forward, (agentPos - doorPos))
    //   dot > 0: agente è sul lato FRONT → entra nel BACK
    //   dot < 0: agente è sul lato BACK  → entra nel FRONT
    //   dot = 0: perpendicolare, default FRONT
    // --------------------------------------------------------

    public string GetRoomNameBeyondDoor(Vector3 agentPos)
    {
        Vector3 toAgent = (agentPos - transform.position).normalized;
        float   dot     = Vector3.Dot(transform.forward, toAgent);
        return dot >= 0f ? roomNameBack : roomNameFront;
    }

    // roomName del lato da cui proviene l'agente
    // (il nodo "from" nell'arco del grafo)
    public string GetRoomNameAgentSide(Vector3 agentPos)
    {
        Vector3 toAgent = (agentPos - transform.position).normalized;
        float   dot     = Vector3.Dot(transform.forward, toAgent);
        return dot >= 0f ? roomNameFront : roomNameBack;
    }

    
   // --------------------------------------------------------
    // AZIONE FISICA: APRI PORTA
    // --------------------------------------------------------
    public bool TryOpen()
{
    if (elementType == ElementType.Varco) return true;
    if (isLocked) return false;

    isOpen = true;

    if (physicalModel != null)
    {
        var doorScript = physicalModel.GetComponent<DoorScript.Door>();
        if (doorScript != null)
            doorScript.open = true;
        else
            physicalModel.SetActive(false);
    }

    NotifyPhysicalStateChanged();
    return true;
}

    public void Close()
    {
        if (elementType == ElementType.Varco) return;
        
        isOpen = false;
        
        
        if (physicalModel != null)
        {
            physicalModel.SetActive(true);
        }

        NotifyPhysicalStateChanged();
    }

    private void NotifyPhysicalStateChanged()
    {
        // Il DoorVarcoArtifactBridge fa polling ogni frame su isOpen/isLocked
        // e notifica JaCaMo automaticamente quando rileva un cambio.
        // Non serve fare nulla qui — il bridge se ne occupa.
        // Questo metodo esiste solo come hook per future estensioni.
    }

    // --------------------------------------------------------
    // RESET
    // --------------------------------------------------------
    public void ResetExplorationData()
    {
        distFromA  = -1f;
        distFromB  = -1f;
        side       = "";
        orderFW    = -1;
        discovered = false;
    }

    // --------------------------------------------------------
    // DISTANZA NAVMESH
    // --------------------------------------------------------
    public float NavMeshDistanceTo(Vector3 from)
    {
        var path = new NavMeshPath();
        if (NavMesh.CalculatePath(from, transform.position, NavMesh.AllAreas, path)
            && path.status == NavMeshPathStatus.PathComplete)
        {
            float d = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                d += Vector3.Distance(path.corners[i-1], path.corners[i]);
            return d;
        }
        return Vector3.Distance(from, transform.position);
    }

  

    private void OnValidate()
    {
        if (elementType == ElementType.Varco)
        {
            isOpen   = true;
            isLocked = false;
        }

       if (roomFront != null && roomBack != null)
        {
            if (IsCorridorLike(roomFront.name) && IsCorridorLike(roomBack.name))
                isCorridorLink = true;
        }
    }
}
