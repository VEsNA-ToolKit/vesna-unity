using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ============================================================
// ExplorationVisionCone.cs
// ============================================================
// Estende VisionCone (originale, non modificato).
//
// PERCHÉ ESISTE:
// La prof vuole un unico componente che faccia sia il sensore
// visivo per corridoi/porte (ex-VisionCone v2) sia il rilevamento
// delle porte durante il transito A→B (ex-DoorSensor).
// VisionCone base rimane intatto — questa è la sottoclasse.
//
// COSA AGGIUNGE AL BASE:
//
//   1. MODALITÀ ESPLORAZIONE (explorationMode)
//      SetExplorationMode(true/false) — chiamato da ExplorationManager.
//      Quando false, il cono non fa nulla di nuovo (pianificazione JaCaMo).
//
//   2. RILEVAMENTO POLI CORRIDOIO (ex-VisionCone v2)
//      Scansiona il layer "corridor" (CorridorPoleScript).
//      Evento statico: OnCorridorPoleVisible(corridorId, poleId, pos)
//      Mantiene memoria dei poli già segnalati.
//
//   3. RILEVAMENTO PORTE IN STANZA (ex-VisionCone v2)
//      Scansiona il layer "doors" durante il 360° in stanza.
//      Evento statico: OnDoorVisibleInRoom(DoorVarcoScript)
//      Attivo solo quando ExplorationManager.inRoomMode == true.
//
//   4. SCAN TRANSITO A→B (ex-DoorSensor)
//      StartTransitScan / StopTransitScan — chiamati da ExplorationManager
//      quando l'agente tocca Polo A / Polo B.
//      Rileva porte sul layer "doors" durante il cammino,
//      calcola distFromA, distFromB, side, orderFW.
//      Evento statico: OnDoorDiscovered(DoorVarcoScript, corridorId, orderFW)
//
// SETUP INSPECTOR (stesso del VisionCone base + nuovi campi):
//   doorLayers      — layer "doors"    (DoorVarcoScript)
//   corridorLayers  — layer "corridor" (CorridorPoleScript)
//   occlusionLayers — muri che bloccano la vista
//   explorationMode — toggle iniziale
//   transitScanFrequency — Hz della scansione durante il transito
//
// RIFERIMENTO ESTERNO:
//   ExplorationManager deve:
//     - assegnare explorationVisionCone invece dei vecchi visionCone + doorSensor
//     - abbonarsi a ExplorationVisionCone.OnCorridorPoleVisible
//     - abbonarsi a ExplorationVisionCone.OnDoorVisibleInRoom
//     - abbonarsi a ExplorationVisionCone.OnDoorDiscovered
//     - chiamare SetExplorationMode(true) all'avvio
//     - chiamare StartTransitScan / StopTransitScan ai poli A/B
// ============================================================

[ExecuteInEditMode]
public class ExplorationVisionCone : VisionCone
{
    // ── Layer aggiuntivi (il base ha già "layers" e "occlusionLayers") ────────
    [Header("Layer Esplorazione")]
    public LayerMask doorLayers;        // layer "doors"    — DoorVarcoScript
    public LayerMask corridorLayers;    // layer "corridor" — CorridorPoleScript
    public LayerMask objectLayers; 

    [Header("Modalità")]
    public bool explorationMode = false;

    [Header("Transito A→B — ex DoorSensor")]
    public int transitScanFrequency = 10;

    // ── Stato interno — scan principale (propri timer, base è private) ────────
    private float exploScanInterval;
    private float exploScanTimer;

    // ── Stato interno — rilevamento poli ──────────────────────────────────────
    private HashSet<string>     reportedPoles = new HashSet<string>();
    private ExplorationManager  explorationManager;

    // ── Stato interno — scan transito (ex-DoorSensor) ─────────────────────────
    private bool      isTransitScanning    = false;
    private Vector3   transitPoleAPos;
    private Vector3   transitPoleBPos;
    private string    transitCorridorId;
    private int       transitFwCounter     = 1;
    private HashSet<string> transitSeen    = new HashSet<string>();
    private float     transitScanInterval;
    private float     transitScanTimer;

    // ── EVENTI STATICI ────────────────────────────────────────────────────────

    /// <summary>
    /// L'agente vede un polo di corridoio non ancora segnalato.
    /// ExplorationManager si abbona per avvicinarsi.
    /// </summary>
    public static new event System.Action<string, string, Vector3> OnCorridorPoleVisible;

    /// <summary>
    /// L'agente vede una porta/varco durante la rotazione 360° in stanza.
    /// ExplorationManager si abbona per aggiungere l'arco al grafo.
    /// </summary>
    public static new event System.Action<DoorVarcoScript> OnDoorVisibleInRoom;

    /// <summary>
    /// Porta scoperta durante il transito A→B (ex-DoorSensor.OnDoorDiscovered).
    /// ExplorationManager si abbona per registrarla nel grafo.
    /// </summary>
    public static event System.Action<DoorVarcoScript, string, int> OnDoorDiscovered;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    // NOTA: Start() e Update() del VisionCone base sono private, quindi non
    // possiamo chiamare base.Start() / base.Update(). Reinizializziamo qui
    // tutto ciò che serve, usando i campi ereditati (distance, angle, height,
    // scanFrequency, scanInterval, scanTimer) che sono protected/public nel base.

    protected new void Start()
    {
        // Replica la logica di VisionCone.Start() (che è private nel base)
        explorationManager  = transform.parent?.GetComponent<ExplorationManager>();
        // Usiamo campi propri per il timer (i campi del base sono private)
        exploScanInterval   = 1f / Mathf.Max(1, scanFrequency);
        transitScanInterval = 1f / Mathf.Max(1, transitScanFrequency);
    }

    protected new void Update()
{
    if (!Application.IsPlaying(gameObject)) return;

    // ── AGGIUNTA: chiama Scan() del base per roomNodeLayers e OnRoomNodeVisible ──
    // Necessario perché base.Update() è private — ExplorationVisionCone lo nasconde
    // con new, quindi il Scan() base non verrebbe mai chiamato senza questa riga.
    Scan();
    // ──────────────────────────────────────────────────────────────────────────────

    if (explorationMode)
    {
        exploScanTimer -= Time.deltaTime;
        if (exploScanTimer < 0f)
        {
            exploScanTimer += exploScanInterval;
            ScanForCorridorPoles();
            ScanForRoomObjects();
            if (explorationManager != null && explorationManager.inRoomMode)
                ScanForDoorsInRoom();
        }
    }

    if (isTransitScanning)
    {
        transitScanTimer -= Time.deltaTime;
        if (transitScanTimer <= 0f)
        {
            transitScanTimer = transitScanInterval;
            TransitScan();
        }
    }
}

    // ── API pubblica — Modalità esplorazione ──────────────────────────────────

    /// <summary>
    /// Abilita/disabilita il sensore di esplorazione.
    /// Chiamato da ExplorationManager. Durante la pianificazione JaCaMo
    /// il cono è spento (explorationMode = false).
    /// </summary>
    public void SetExplorationMode(bool active)
    {
        explorationMode = active;
        if (!active) reportedPoles.Clear();
        Debug.Log($"[ExplorationVisionCone] explorationMode = {active}");
    }

    /// <summary>
    /// Resetta la memoria dei poli segnalati.
    /// Chiamato da ExplorationManager quando inizia un nuovo corridoio.
    /// </summary>
    public void ResetPoleMemory() => reportedPoles.Clear();

    // ── API pubblica — Scan transito (ex-DoorSensor) ──────────────────────────

    /// <summary>
    /// Avvia la scansione porte durante il transito A→B.
    /// Chiamato da ExplorationManager quando l'agente tocca il Polo A.
    /// </summary>
    public void StartTransitScan(Vector3 poleA, Vector3 poleB, string corridorId)
    {
        transitPoleAPos     = poleA;
        transitPoleBPos     = poleB;
        transitCorridorId   = corridorId;
        isTransitScanning   = true;
        transitFwCounter    = 1;
        transitSeen.Clear();
        Debug.Log($"[ExplorationVisionCone] TransitScan avviato per {corridorId}");
    }

    /// <summary>
    /// Ferma la scansione transito.
    /// Chiamato da ExplorationManager quando l'agente tocca il Polo B.
    /// </summary>
    public void StopTransitScan()
    {
        isTransitScanning = false;
        Debug.Log("[ExplorationVisionCone] TransitScan fermato.");
    }

    // ── Scan: poli corridoio ──────────────────────────────────────────────────

    private void ScanForCorridorPoles()
    {
        Collider[] buffer = new Collider[50];
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, distance, buffer,
            corridorLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            var go   = buffer[i].gameObject;
            var pole = go.GetComponent<CorridorPoleScript>();
            if (pole == null) continue;

            string key = $"{pole.corridorId}_{pole.poleId}";
            if (reportedPoles.Contains(key)) continue;
            if (!IsInSight(go)) continue;

            reportedPoles.Add(key);
            Debug.Log($"[ExplorationVisionCone] Polo visibile: {key}");
            OnCorridorPoleVisible?.Invoke(pole.corridorId, pole.poleId, go.transform.position);
        }
    }

    // ── Scan: porte in stanza (durante 360°) ──────────────────────────────────

    private void ScanForDoorsInRoom()
{
    Collider[] buffer = new Collider[50];
    int doorCount = Physics.OverlapSphereNonAlloc(
        transform.position, distance, buffer,
        doorLayers, QueryTriggerInteraction.Collide);

    for (int i = 0; i < doorCount; i++)
    {
        var go = buffer[i].gameObject;
        var door = go.GetComponentInParent<DoorVarcoScript>();
        if (door == null) continue;
        if (door.discovered) continue;
        if (!IsInSight(go)) continue;

        OnDoorVisibleInRoom?.Invoke(door);
    }
}

    // ── Scan: porte durante transito A→B (ex-DoorSensor) ─────────────────────

    private void TransitScan()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, distance,
            doorLayers
        );

        foreach (Collider col in hits)
        {
            var door = col.GetComponentInParent<DoorVarcoScript>();
            if (door == null) continue;

            string doorName = door.gameObject.name;
            if (transitSeen.Contains(doorName)) continue;

            // Filtro distanza: scarta porte oltre il Polo B
            float dAgentToB    = Vector3.Distance(transform.position, transitPoleBPos);
            float dAgentToDoor = Vector3.Distance(transform.position, door.transform.position);
            if (dAgentToDoor > dAgentToB + 0.55f) continue;

            // Calcola geometria
            door.distFromA  = door.NavMeshDistanceTo(transitPoleAPos);
            door.distFromB  = door.NavMeshDistanceTo(transitPoleBPos);
            door.orderFW    = transitFwCounter;
            door.discovered = true;

            // Lato: prodotto vettoriale forward agente × direzione porta
            Vector3 toDoor = (door.transform.position - transform.position).normalized;
            Vector3 cross  = Vector3.Cross(transform.forward, toDoor);
            door.side = cross.y >= 0 ? "RIGHT" : "LEFT";

            transitSeen.Add(doorName);

            // Notifica ExplorationManager
            OnDoorDiscovered?.Invoke(door, transitCorridorId, transitFwCounter);
            transitFwCounter++;

            Debug.Log($"[ExplorationVisionCone] Porta transito: {doorName} " +
                      $"dA={door.distFromA:F1} dB={door.distFromB:F1} " +
                      $"side={door.side} fw={door.orderFW}");
        }
    }

    // ── IsInSight — override che usa i layer corretti ─────────────────────────
    // Il base usa "layers" generico (shop). Qui usiamo doorLayers + corridorLayers
    // per l'occlusione, coerente con la versione v2.

    
    public new bool IsInSight(GameObject targetObj)
{
    Vector3 origin = transform.position;
    origin.y += height / 2f;

    Vector3 dest = targetObj.transform.position;
    dest.y = origin.y;

    Vector3 direction = dest - origin;
    float dist = direction.magnitude;

    if (Vector3.Angle(direction, transform.forward) > angle) return false;

    RaycastHit hit;
    if (Physics.Raycast(origin, direction.normalized, out hit, dist,
                        occlusionLayers | doorLayers))
    {
        if (hit.collider.gameObject != targetObj)
        {
            // Ignora il collider della porta target stessa e suoi figli
            if (hit.collider.GetComponentInParent<DoorVarcoScript>() == 
                targetObj.GetComponentInParent<DoorVarcoScript>())
                return true;
                
            if (hit.collider.GetComponentInParent<CorridorPoleScript>() == null)
                return false;
        }
    }
    return true;
}

// ── AGGIUNTA: HashSet per tenere traccia degli artefatti già visti ────────
private HashSet<string> _seenObjects = new HashSet<string>();
private Dictionary<string, string> _lastSeenRoom = new Dictionary<string, string>();

private void ScanForRoomObjects()
{
    Collider[] buffer = new Collider[50];
    int count = Physics.OverlapSphereNonAlloc(
        transform.position, distance, buffer,
        objectLayers, QueryTriggerInteraction.Collide);

    for (int i = 0; i < count; i++)
    {
        // ← sostituisci RoomObjectArtifact con Artifact
        var artifact = buffer[i].GetComponent<Artifact>();
        if (artifact == null) continue;
        if (!IsInSight(buffer[i].gameObject)) continue;

        string id     = artifact.gameObject.name;
        string roomId = artifact.roomId;

        if (!_seenObjects.Contains(id))
        {
            Debug.Log($"[EVC] '{id}' VISTO in '{roomId}'");

            explorationManager?.RegisterDiscoveredObject(
                id, roomId, artifact.ArtifactType.ToString(),
                int.Parse(artifact.port),
                artifact.transform.position.x,
                artifact.transform.position.y,
                artifact.transform.position.z);

            var bt = transform.parent?.GetComponent<BeliefTransmitter>();
            bt?.SendRoomObjectDiscovered(
                id, roomId, artifact.ArtifactType.ToString(),
                int.Parse(artifact.port),
                artifact.transform.position.x,
                artifact.transform.position.y,
                artifact.transform.position.z);

            _seenObjects.Add(id);
            _lastSeenRoom[id] = roomId;
        }
        else if (_lastSeenRoom.TryGetValue(id, out var lastRoom) && lastRoom != roomId)
        {
            explorationManager?.RegisterDiscoveredObject(
                id, roomId, artifact.ArtifactType.ToString(),
                int.Parse(artifact.port),
                artifact.transform.position.x,
                artifact.transform.position.y,
                artifact.transform.position.z);

            _lastSeenRoom[id] = roomId;
        }
    }
}

}
