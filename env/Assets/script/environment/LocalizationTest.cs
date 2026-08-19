using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LocalizationTest : MonoBehaviour
{
    public VisionCone visionCone;   // il cono di QUESTO avatar (assegnare in inspector)

    [Header("Localizzazione tramite collider")]
    public LayerMask roomColliderMask;   // layer "RoomNode" (= roomNodeLayers del cono)
    public float searchRadius = 100f;    // raggio entro cui cercare i collider stanza

    private Dictionary<string, Vector3> seenRooms = new Dictionary<string, Vector3>();
    private string currentRoom = null;
    private AgentJacamoBridge _bridge;
    
    public string CurrentRoom => currentRoom;

    public void SetCurrentRoom(string roomId)
    {
        currentRoom = roomId;
    }

    void Start()
    {
        if (visionCone != null) visionCone.OnRoomNodeVisible += OnRoomSeen;
        _bridge = GetComponent<AgentJacamoBridge>();
        
        // Esegue la localizzazione una volta all'avvio
        RunLocalization();
    }

    void OnDestroy()
    {
        if (visionCone != null) visionCone.OnRoomNodeVisible -= OnRoomSeen;
    }

    // Metodo pubblico: puoi chiamarlo da altri script se serve ri-localizzare a comando
    public void RunLocalization()
    {
        StopAllCoroutines(); // Ferma eventuali ricerche e invii precedenti
        seenRooms.Clear();   // Pulisce le stanze viste in precedenza
        StartCoroutine(LocalizeRoutine());
    }
    // Forza la stanza nota (da ExplorationManager) e la re-invia subito.
    // Bypassa cono + coroutine di retry, così nessun LastArrivedNode stantio
    // può sovrascrivere il nodo corretto a fine esplorazione.
    public void ForceCurrentRoomAndSend(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return;

        StopAllCoroutines();   // ferma eventuali invii precedenti (quello dell'avvio)
        seenRooms.Clear();
        currentRoom = roomId;

        Debug.Log($"[Localization] current_room aggiornata a fine esplorazione = {roomId}");
        _bridge?.SendCurrentRoom(roomId);
    }

    private void OnRoomSeen(string roomName, Vector3 position)
    {
        if (visionCone == null) return;

        GameObject go = ResolveRoomNode(roomName, position);
        if (go == null) return; 
        if (!visionCone.IsInSight(go)) return; 

        if (!seenRooms.ContainsKey(roomName))
        {
            seenRooms[roomName] = position;
        }
    }

    // Recupera il GameObject del room node dalla sua posizione
    private GameObject ResolveRoomNode(string roomName, Vector3 position)
    {
        Collider[] near = Physics.OverlapSphere(
            position, 0.2f, roomColliderMask, QueryTriggerInteraction.Collide);

        foreach (var c in near)
            if (c.gameObject.name == roomName)
                return c.gameObject;

        return null;
    }

    private IEnumerator LocalizeRoutine()
    {
        yield return new WaitForSeconds(1f);

        // *** AGGIUNTA: campiona collider iniziale prima della rotazione ***
        Collider[] overlapping = Physics.OverlapSphere(
            transform.position, 0.1f, roomColliderMask, QueryTriggerInteraction.Collide);
        string insideCollider = overlapping.Length > 0 ? overlapping[0].gameObject.name : null;

        // 360° per far entrare nel cono tutte le stanze intorno
        float rotated = 0f;
        float speed = 90f;
        while (rotated < 360f)
        {
            float step = speed * Time.deltaTime;
            transform.Rotate(0, step, 0);
            rotated += step;
            yield return null;
        }

        // aspetta che almeno una stanza venga vista
        float timeout = 5f;
        while (seenRooms.Count == 0 && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // *** AGGIUNTA: se era dentro un collider usa quello, altrimenti comportamento originale ***
        if (insideCollider != null)
        {
            currentRoom = insideCollider;
            Debug.Log($"[Localization] dentro collider: {currentRoom}");
        }
        else
        {
            if (seenRooms.Count == 0) yield break;
            currentRoom = NearestVisibleRoomByCollider();
        }

        if (currentRoom == null) yield break;
    
        // L'UNICO LOG STAMPATO
        Debug.Log($"stanza corrente = {currentRoom}");
        
        // Avvia l'invio ripetuto a tempo (10 secondi totali)
        StartCoroutine(SendRoomDataWithRetries());
    }

    // Tra le sole stanze VISTE dal mio cono, sceglie la più vicina per distanza sul collider
    private string NearestVisibleRoomByCollider()
    {
        Collider[] cols = Physics.OverlapSphere(
            transform.position, searchRadius, roomColliderMask, QueryTriggerInteraction.Collide);

        string best = null;
        float minDist = float.MaxValue;

        foreach (var col in cols)
        {
            if (!seenRooms.ContainsKey(col.gameObject.name)) continue;

            Vector3 cp = col.ClosestPoint(transform.position);
            float d = Vector3.Distance(transform.position, cp);

            if (d < minDist)
            {
                minDist = d;
                best = col.gameObject.name;   
                if (d == 0f) break;           
            }
        }

        if (best == null)
        {
            foreach (var kv in seenRooms)
            {
                float d = Vector3.Distance(transform.position, kv.Value);
                if (d < minDist) { minDist = d; best = kv.Key; }
            }
        }

        return best;
    }

    // Trasmette la stanza corrente ogni 3 secondi per un massimo di 10 secondi
    private IEnumerator SendRoomDataWithRetries()
    {
        float elapsed = 0f;
        float maxDuration = 10f; // Durata massima dei tentativi

        while (elapsed <= maxDuration)
        {
            string roomToSend = _bridge?.LastArrivedNode ?? currentRoom;

            if (roomToSend != null)
            {
                _bridge?.SendCurrentRoom(roomToSend);
            }

            yield return new WaitForSeconds(3f);
            elapsed += 3f;
        }
    }
}