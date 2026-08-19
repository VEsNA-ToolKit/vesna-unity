using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;  
using System.Linq;

// ============================================================
// ExplorationManager.cs  — v2.4
// MODIFICHE rispetto a v2.3:
//   ARCHITETTURA MULTI-GRAFO (un TopologicalGraph per piano)
//
//   • floorGraphs[N] contiene solo i nodi del piano N.
//   • La proprietà `graph` restituisce sempre floorGraphs[currentFloor],
//     quindi tutto il codice di esplorazione resta invariato.
//   • Quando l'agente attraversa un connettore verticale, ExecuteFloorChange:
//       1. Aggiunge "conn_{connectorId}" al grafo piano CORRENTE
//          (pos = triggerStartPosition, es. Door_Cor1-Box su piano 1).
//       2. Crea/recupera il grafo piano DESTINAZIONE.
//       3. Aggiunge "conn_{connectorId}" al grafo piano DESTINAZIONE
//          (pos = triggerEndPosition, es. Door_Box su piano 0).
//       4. Registra il link in ConnectorLinks (struttura pubblica).
//   • BeliefTransmitter può accedere ad AllFloorGraphs e ConnectorLinks
//     per ricostruire il grafo completo dell'edificio.
// ============================================================

// ─────────────────────────────────────────────────────────────
// STRUTTURA DATI: link cross-floor tra due grafi di piano
// ─────────────────────────────────────────────────────────────
[System.Serializable]
public struct FloorConnectorLink
{
    public string  connectorId;
    public int     floorFrom;
    public string  nodeIdFrom;    // ID nodo in floorGraphs[floorFrom]
    public Vector3 posFrom;       // posizione fisica lato partenza
    public int     floorTo;
    public string  nodeIdTo;      // ID nodo in floorGraphs[floorTo]
    public Vector3 posTo;         // posizione fisica lato arrivo
     public bool    bidirectional;
}


// ─────────────────────────────────────────────────────────────

public class ExplorationManager : MonoBehaviour
{
    [Header("Agente")]
    public NavMeshAgent          navAgent;
    public ExplorationVisionCone explorationVisionCone;

    [Header("Macro-Grafo edificio")]
    public BuildingGraph buildingGraph;


    [Header("Impostazioni")]

    public float arrivalThreshold      = 0.5f;
    public float poleArrivalThreshold  = 1.2f;
    public float rotationSpeed         = 120f;
    public float inspectionDistance    = 1.2f;
    public float enterOffset           = 1.5f;
    public float poleTimeoutSeconds    = 4f;

    [Header("Persistenza")]
    public string buildingId = "building";  // es. "PalazzoRossi_P1"

    [ContextMenu("Apri cartella snapshot")]
    private void OpenSnapshotFolder()
    {
        System.Diagnostics.Process.Start(GraphPersistence.BaseDirectory);
    }

    // --------------------------------------------------------
    // MACCHINA A STATI
    // --------------------------------------------------------
    private enum State
    {
        Idle, MovingToPole, Transiting, WaitingAtPoleB,
        Rotating360, MovingToDoor, InspectingDoor,
        EnteringRoom, InsideRoom, Backtracking,
        MovingToConnector, Completed, TraversingStairs
    }

    private State state = State.Idle;

    // ─────────────────────────────────────────────────────────
    // MULTI-GRAFO: un TopologicalGraph per piano
    // ─────────────────────────────────────────────────────────
    private Dictionary<int, TopologicalGraph> floorGraphs
        = new Dictionary<int, TopologicalGraph>();

    /// <summary>Grafo del piano corrente (proprietà calcolata).</summary>
    private TopologicalGraph graph
        => floorGraphs.TryGetValue(currentFloor, out var g) ? g : null;

    /// <summary>Tutti i grafi, accessibili da BeliefTransmitter.</summary>
    public Dictionary<int, TopologicalGraph> AllFloorGraphs => floorGraphs;

    /// <summary>
    /// Link cross-floor: descrive come il nodo conn_X nel grafo pianoA si
    /// collega al nodo conn_X nel grafo pianoB. Usato da BeliefTransmitter
    /// per ricostruire il grafo completo dell'edificio.
    /// </summary>
    public List<FloorConnectorLink> ConnectorLinks { get; }
        = new List<FloorConnectorLink>();

    // Alias pubblico per retrocompatibilità
    public TopologicalGraph Graph => graph;

    private string currentNodeId;
    public  string CurrentNodeId => currentNodeId;

    private string  transitCorridorId;
    private Vector3 poleAPos;
    private Vector3 poleBPos;

    private string  pendingCorridorId;
    private string  pendingPoleId;
    private Vector3 pendingPolePos;

    private GraphEdge currentEdge;

    private float rotationAccum = 0f;
    public  bool  inRoomMode   = false;

    private Dictionary<string, string> registeredRoomDoors = new Dictionary<string, string>();
    private HashSet<string> registeredTransitDoors = new HashSet<string>();
    private HashSet<string> allCorridorDoors       = new HashSet<string>();

    private bool explorationStarted = false;
    public int  currentFloor;

    private string lastCorridorLinkDoorName;
    private VerticalConnector pendingConnector;

    private DoorVarcoScript pendingArrivalDoor = null;

    private HashSet<string>   traversedConnectors = new HashSet<string>();

    private Dictionary<string, HashSet<string>> _roomArtifacts
    = new Dictionary<string, HashSet<string>>();
private Dictionary<string, RoomObjectSnapshot> _artifactData
    = new Dictionary<string, RoomObjectSnapshot>();

    private string movingToPoleCorridorId;
    private float  movingToPoleTimer = 0f;
    private float  backtrackTimer    = 0f;
    public  float  backtrackTimeout  = 5f;

    private float transitTimer = 0f;

    private float idleTimer = 0f;

    // Corridoio verso cui l'agente sta navigando in stato Idle
    // (dopo EnterThroughEdge su un varco corridoio). Blocca segnali
    // di poli di altri corridoi finché non si tocca il polo corretto.
    private string idleTargetCorridorId;

    // --------------------------------------------------------
    // EVENTI
    // --------------------------------------------------------
    void Awake()
    {
        if (buildingGraph != null)
            foreach (var floor in buildingGraph.floors)
                floor.explored = false;

        CorridorPoleScript.OnPoleTouched            += HandlePoleTouched;
        ExplorationVisionCone.OnDoorDiscovered      += HandleDoorDiscovered;
        ExplorationVisionCone.OnCorridorPoleVisible += HandleCorridorPoleVisible;
        ExplorationVisionCone.OnDoorVisibleInRoom   += HandleDoorVisibleInRoom;
        VerticalConnectorScript.OnConnectorReached  += HandleConnectorReached;
        
    }

    void OnDestroy()
    {
        CorridorPoleScript.OnPoleTouched            -= HandlePoleTouched;
        ExplorationVisionCone.OnDoorDiscovered      -= HandleDoorDiscovered;
        ExplorationVisionCone.OnCorridorPoleVisible -= HandleCorridorPoleVisible;
        ExplorationVisionCone.OnDoorVisibleInRoom   -= HandleDoorVisibleInRoom;
        VerticalConnectorScript.OnConnectorReached  -= HandleConnectorReached;
        
    }

    // --------------------------------------------------------
    // AVVIO
    // --------------------------------------------------------
    void Start()
    {
        if (buildingGraph == null)
        {
            Debug.LogWarning("[ExplMgr] BuildingGraph non assegnato!");
            return;
        }
    
        // ── Controlla se esiste già uno snapshot su disco ────────
        if (GraphPersistence.Exists(buildingId))
        {
            BuildingSnapshot snapshot = GraphPersistence.Load(buildingId);
    
            if (snapshot != null)
            {
                Debug.Log($"[ExplMgr] Snapshot trovato per '{buildingId}': " +
                        $"trasmissione diretta a JaCaMo, esplorazione saltata.");
    
                var bt = GetComponent<BeliefTransmitter>();
                bt?.TransmitSnapshot(snapshot);
    
                // Metti la macchina a stati in Completed:
                // l'agente resta fermo, JaCaMo ha già tutto.
                state = State.Completed;
                return;
            }
            else
            {
                // File corrotto o illeggibile: procedi con esplorazione normale
                Debug.LogWarning($"[ExplMgr] Snapshot corrotto per '{buildingId}': riesplorazione.");
            }
        }
    
        // ── Nessuno snapshot: avvio esplorazione normale ─────────
        foreach (var floor in buildingGraph.floors)
            floor.explored = false;
    
        Vector3   agentPos  = navAgent.transform.position;
        FloorNode floorData = null;
        float     minDist   = float.MaxValue;
    
        foreach (var floor in buildingGraph.floors)
        {
            foreach (var room in floor.roomObjects)
            {
                if (room == null) continue;
                float d = Vector3.Distance(agentPos, room.transform.position);
                if (d < minDist) { minDist = d; floorData = floor; }
            }
        }
    
        if (floorData == null || floorData.spawnObject == null)
        {
            Debug.LogWarning("[ExplMgr] Piano non trovato o spawnObject mancante!");
            return;
        }
    
        Vector3 startPos = floorData.spawnPosition;
        if (Vector3.Distance(agentPos, startPos) > 0.05f)
        {
            navAgent.enabled = false;
            navAgent.transform.position = startPos;
            navAgent.enabled = true;
            navAgent.Warp(startPos);
        }
    
        currentFloor = floorData.floorIndex;
        StartExploration(floorData.spawnObject.name, startPos);
    }
    // ──────────────────────────────────────────────────────────────
// METODO 1: StartExploration  (sostituisce l'intero metodo)
// ──────────────────────────────────────────────────────────────
public void StartExploration(string startNodeId, Vector3 startPos)
{
    if (explorationStarted) return;
    explorationStarted = true;
 

    Debug.Log($"[ExplMgr] Piano di partenza: {currentFloor}");
 
    // Crea il grafo per il piano iniziale
    floorGraphs[currentFloor] = new TopologicalGraph();
 
    explorationVisionCone?.SetExplorationMode(true);
    currentNodeId = startNodeId;
CorridorPoleScript.OnPoleTouched -= HandlePoleTouched;
CorridorPoleScript.OnPoleTouched += HandlePoleTouched;

var spawnCorridor = FindCorridorData(startNodeId);
if (spawnCorridor != null)
{
    // Spawn in un corridoio: non creare un room node,
    // vai direttamente al polo più vicino
    float d1 = Vector3.Distance(startPos, spawnCorridor.Pole1Position);
    float d2 = Vector3.Distance(startPos, spawnCorridor.Pole2Position);
    pendingCorridorId      = startNodeId;
    pendingPoleId          = d1 <= d2 ? "1" : "2";
    pendingPolePos         = d1 <= d2 ? spawnCorridor.Pole1Position : spawnCorridor.Pole2Position;
    movingToPoleCorridorId = startNodeId;
    movingToPoleTimer      = 0f;
    navAgent.isStopped     = false;
    navAgent.SetDestination(pendingPolePos);
    state = State.MovingToPole;
}
else
{
    graph.AddRoomNode(startNodeId, startPos);
    graph.EnterNode(startNodeId);
    StartRotation360(isRoom: true);
}

Debug.Log($"[ExplMgr] Avviato da '{startNodeId}' piano {currentFloor}");
}

    // --------------------------------------------------------
    // UPDATE
    // --------------------------------------------------------
    void Update()
    {
        switch (state)
        {
            case State.MovingToPole:      UpdateMovingToPole();      break;
            case State.Transiting:        UpdateTransiting();        break;
            case State.Rotating360:       UpdateRotating360();       break;
            case State.MovingToDoor:      UpdateMovingToDoor();      break;
            case State.InspectingDoor:    UpdateInspectingDoor();    break;
            case State.EnteringRoom:      UpdateEnteringRoom();      break;
            case State.InsideRoom:        UpdateInsideRoom();        break;
            case State.Idle:          UpdateIdle();          break;
            case State.Backtracking:  UpdateBacktracking();  break;
            case State.MovingToConnector: UpdateMovingToConnector(); break;
        }
    }

    // --------------------------------------------------------
    // HANDLER: VisionCone vede un polo di corridoio
    // --------------------------------------------------------
    private void HandleCorridorPoleVisible(string corridorId, string poleId, Vector3 polePos)
    {
        if (state == State.MovingToPole || state == State.Transiting) return;
        if (state == State.Rotating360) return;
        if (state != State.Idle && state != State.Backtracking) return;
        if (!string.IsNullOrEmpty(idleTargetCorridorId) && corridorId != idleTargetCorridorId) return;

        var corridorData = FindCorridorData(corridorId);
        if (corridorData == null) return;

        Vector3 myPos   = transform.position;
        float distPole1 = Vector3.Distance(myPos, corridorData.Pole1Position);
        float distPole2 = Vector3.Distance(myPos, corridorData.Pole2Position);

        Vector3 nearestPolePos;
        string  nearestPoleId;
        if (distPole1 <= distPole2)
        { nearestPolePos = corridorData.Pole1Position; nearestPoleId = "1"; }
        else
        { nearestPolePos = corridorData.Pole2Position; nearestPoleId = "2"; }

        if (movingToPoleCorridorId == corridorId) return;

        pendingCorridorId      = corridorId;
        pendingPoleId          = nearestPoleId;
        pendingPolePos         = nearestPolePos;
        movingToPoleCorridorId = corridorId;

        navAgent.isStopped = false;
        navAgent.SetDestination(nearestPolePos);
        movingToPoleTimer = 0f;

        state = State.MovingToPole;

        Debug.Log($"[ExplMgr] Polo {corridorId}/{nearestPoleId} individuato → mi avvicino.");
    }

    // --------------------------------------------------------
    // HANDLER: Polo fisicamente toccato
    // --------------------------------------------------------
    private void HandlePoleTouched(string corridorId, string poleId, Vector3 polePos)
    {
        Debug.Log($"[ExplMgr] Segnale fisico da {corridorId}_{poleId}. Stato: {state}");

       if (state == State.Transiting && corridorId == transitCorridorId)
        {
            // Ignora segnali di polo se siamo ancora vicini al polo A:
            // significa che è un polo diverso da B che ha fatto scattare il trigger
            // prima che l'agente si sia davvero avvicinato a B.
            float distToB = Vector3.Distance(transform.position, poleBPos);
            float distToA = Vector3.Distance(transform.position, poleAPos);
            if (distToA < distToB && distToB > poleArrivalThreshold)
            {
                return;
            }
            pendingCorridorId = corridorId;
            pendingPoleId     = poleId;
            pendingPolePos    = polePos;
            ProcessPoleTouched();
            return;
        }

        if (state == State.MovingToPole && corridorId == pendingCorridorId)
        {
            pendingPolePos = polePos;
            ProcessPoleTouched();
            return;
        }

        if (state == State.Idle)
        {
            // Se stiamo navigando verso un corridoio specifico (idleTargetCorridorId),
            // ignora segnali di polo di altri corridoi per non interrompere il tragitto.
            if (!string.IsNullOrEmpty(idleTargetCorridorId) && corridorId != idleTargetCorridorId)
            {
                ResetCorridorPoles(corridorId);
                return;
            }

            // Salta il check "già transitato" se ci stiamo andando intenzionalmente:
            // il corridoio può avere ancora porte da esplorare anche se il transito
            // centrale è già marcato come esplorato.
            bool isIntentionalTarget = !string.IsNullOrEmpty(idleTargetCorridorId)
                                    && corridorId == idleTargetCorridorId;

            if (!isIntentionalTarget)
            {
                var poleANode = graph.GetCorridorPoleNode($"{corridorId}_A");
                if (poleANode != null)
                {
                    var central = poleANode.edges.Find(e => e?.id == $"central_{corridorId}");
                    /*if (central != null && central.state == EdgeState.Explored)
                    {
                        Debug.Log($"[ExplMgr] Polo {corridorId}_{poleId} ignorato — corridoio già transitato.");
                        return;
                    }*/
                    //NUOVO
                    if (central != null && central.state == EdgeState.Explored)
{
    if (!string.IsNullOrEmpty(currentNodeId) &&
        (currentNodeId.EndsWith("_A") || currentNodeId.EndsWith("_B")))
    {
        string poleAId = $"{corridorId}_A";
        string poleBId = $"{corridorId}_B";
        Vector3 doorPos = transform.position;
        float distToA = Vector3.Distance(doorPos, graph.GetNode(poleAId)?.position ?? doorPos);
        float distToB = Vector3.Distance(doorPos, graph.GetNode(poleBId)?.position ?? doorPos);
        string nearestTo = distToA <= distToB ? poleAId : poleBId;
        graph.AddSegmentArc(currentNodeId, nearestTo,
            Vector3.Distance(transform.position, graph.GetNode(nearestTo)?.position ?? transform.position));
    }
    Debug.Log($"[ExplMgr] Polo {corridorId}_{poleId} ignorato — corridoio già transitato.");
    return;
}
                }
            }

            // Corridoio corretto raggiunto: pulisci il target idle
            idleTargetCorridorId = null;
            pendingCorridorId = corridorId;
            pendingPoleId     = poleId;
            pendingPolePos    = polePos;
            ProcessPoleTouched();
            return;
        }

        ResetCorridorPoles(corridorId);
    }

    // --------------------------------------------------------
    // HANDLER: DoorSensor scopre una porta durante il transito
    // --------------------------------------------------------
    private void HandleDoorDiscovered(DoorVarcoScript door, string corridorId, int orderFW)
    {
        if (state != State.Transiting) return;
        if (corridorId != transitCorridorId) return;
        if (registeredTransitDoors.Contains(door.gameObject.name)) return;
        if (allCorridorDoors.Contains(door.gameObject.name)) return;

        float corridorLength = Vector3.Distance(poleAPos, poleBPos);
        if (door.distFromB > corridorLength + 0.6f && door.distFromA > corridorLength + 0.6f)
        {
            Debug.Log($"[ExplMgr] {door.gameObject.name} oltre il corridoio → ignorata");
            return;
        }

        string roomBeyond = door.GetRoomNameBeyondDoor(transform.position);
        string frontName = door.roomFront?.name ?? "";
        string backName  = door.roomBack?.name  ?? "";
        if (frontName != corridorId && backName != corridorId) return;
        bool isCorrLink = door.isCorridorLink;

        registeredTransitDoors.Add(door.gameObject.name);
        if (!isCorrLink) allCorridorDoors.Add(door.gameObject.name);

        string computedSide = ComputeSideRelativeToCorridorAxis(
            door.transform.position, poleAPos, poleBPos);
        if (!isCorrLink)
        {
            graph.AddDoorEdges(corridorId, door.gameObject.name,
            door.elementType == DoorVarcoScript.ElementType.Door,
            door.transform.position, computedSide,
            door.distFromA, door.distFromB, 0,
            isCorridorLink: isCorrLink);
        }
        

        Debug.Log($"[ExplMgr] Porta censita: {door.gameObject.name} " +
                $"corridoio={corridorId} side={computedSide} corridorLink={isCorrLink}");
    }

    private string ComputeSideRelativeToCorridorAxis(Vector3 doorPos, Vector3 posA, Vector3 posB)
    {
        Vector3 axis   = (posB - posA).normalized; axis.y = 0f;
        Vector3 toDoor = (doorPos - posA).normalized; toDoor.y = 0f;
        float cross = axis.x * toDoor.z - axis.z * toDoor.x;
        if (Mathf.Abs(cross) < 0.05f) return "RIGHT";
        return cross > 0f ? "LEFT" : "RIGHT";
    }

    // --------------------------------------------------------
    // HANDLER: VisionCone vede una porta dentro una stanza
    // --------------------------------------------------------
    private void HandleDoorVisibleInRoom(DoorVarcoScript door)
    {
        if (state != State.Rotating360 && state != State.InsideRoom) return;
        if (!inRoomMode) return;
        if (registeredRoomDoors.TryGetValue(door.gameObject.name, out string registeredFrom)
            && registeredFrom == currentNodeId) return;

        // FIX spam: porte non-connettore in un nodo connettore → ignora UNA VOLTA SOLA
        if (traversedConnectors.Contains(currentNodeId) && !IsConnectorDoor(door.gameObject.name))
        {
            if (!registeredRoomDoors.ContainsKey(door.gameObject.name))
            {
                registeredRoomDoors[door.gameObject.name] = currentNodeId;
            }
            return;
        }

        // ── PORTE CONNETTORE (scale/ascensori) ──
        if (IsConnectorDoor(door.gameObject.name))
        {
            var connectorForDoor = FindConnectorForDoor(door.gameObject.name);

            // La porta è accettata se:
            //   a) appartiene fisicamente a questa stanza (roomFront/roomBack)
            //   b) oppure il nodo corrente È il connettore stesso (es. scalaSolaio vede Door_Solaio2)
            bool frontMatch     = door.roomFront != null && door.roomFront.name == currentNodeId;
            bool backMatch      = door.roomBack  != null && door.roomBack.name  == currentNodeId;
            bool connectorMatch = connectorForDoor != null && connectorForDoor.id == currentNodeId;

            if (!frontMatch && !backMatch && !connectorMatch)
            {
                if (!registeredRoomDoors.ContainsKey(door.gameObject.name))
                {
                    registeredRoomDoors[door.gameObject.name] = currentNodeId;
                   
                }
                return;
            }

            // Connettore già attraversato: creo arco verso la stanza al di là
            if (connectorForDoor != null && traversedConnectors.Contains(connectorForDoor.id))
            {
                if (!registeredRoomDoors.ContainsKey(door.gameObject.name))
                {
                    registeredRoomDoors[door.gameObject.name] = currentNodeId;
                    if (!traversedConnectors.Contains(currentNodeId))
                        return;

                    // Determina la stanza di destinazione
                    string connRoomBeyond = door.GetRoomNameBeyondDoor(transform.position);

                    // FIX self-loop: se GetRoomNameBeyondDoor restituisce il nodo corrente
                    // (succede quando door.roomFront/roomBack non sono configurati correttamente),
                    // cerco la stanza non ancora visitata del piano corrente più vicina alla porta.
                    if (string.IsNullOrEmpty(connRoomBeyond)
                        || connRoomBeyond == currentNodeId
                        || connRoomBeyond == connectorForDoor.id)
                    {
                        var fd = buildingGraph?.GetFloor(currentFloor);
                        if (fd != null)
                        {
                            float minDist = float.MaxValue;
                            string nearest = null;
                            foreach (string roomId in fd.roomIds)
                            {
                                if (roomId == currentNodeId || roomId == connectorForDoor.id) continue;
                                if (graph.GetNode(roomId) != null) continue; // già visitata
                                var rObj = fd.roomObjects.Find(r => r != null && r.name == roomId);
                                if (rObj == null) continue;
                                float d = Vector3.Distance(door.transform.position, rObj.transform.position);
                                if (d < minDist) { minDist = d; nearest = roomId; }
                            }
                            if (!string.IsNullOrEmpty(nearest)) connRoomBeyond = nearest;
                        }
                    }

                    // Se ancora non abbiamo una destinazione valida, abbandoniamo
                    if (string.IsNullOrEmpty(connRoomBeyond)
                        || connRoomBeyond == currentNodeId
                        || connRoomBeyond == connectorForDoor.id)
                        return;

                    string targetId = connRoomBeyond;

                    // Aggiungi il nodo destinazione al grafo se non esiste ancora
                    if (graph.GetNode(targetId) == null)
                    {
                        var floorData = buildingGraph?.GetFloor(currentFloor);
                        var roomObj   = floorData?.roomObjects.Find(r => r != null && r.name == targetId);
                        if (roomObj != null)
                            graph.AddRoomNode(targetId, roomObj.transform.position);
                    }

                    if (graph.GetNode(targetId) == null) return; // nodo non trovabile, skip

                    float connNavDist = door.NavMeshDistanceTo(transform.position);
                    var edge = graph.AddRoomDoorEdge(currentNodeId, door.gameObject.name,
                        door.elementType == DoorVarcoScript.ElementType.Door,
                        door.transform.position, connNavDist, targetId);

                    // Marca explored SOLO se la stanza è già stata visitata;
                    // altrimenti resta Discovered → l'agente la raggiungerà via la porta.
                    var beyondNode = graph.GetNode(targetId);
                    bool targetVisited = beyondNode != null && beyondNode.physicallyVisited;
                    if (edge != null && targetVisited)
                        graph.MarkEdgeExplored(currentNodeId, edge.id, targetId);

                    Debug.Log($"[ExplMgr] Connettore '{door.gameObject.name}' già attraversato → " +
                              $"RoomDoor {(targetVisited ? "explored" : "discovered")} verso '{targetId}'.");
                }
                return;
            }

            // Connettore NON ancora attraversato
            string corridorBeyond = door.GetRoomNameBeyondDoor(transform.position);
            if (!string.IsNullOrEmpty(corridorBeyond) && FindCorridorData(corridorBeyond) != null)
            {
                if (!registeredRoomDoors.ContainsKey(door.gameObject.name))
                {
                    registeredRoomDoors[door.gameObject.name] = currentNodeId;
                    NavMeshPath path = new NavMeshPath();
                    float corridorNavDist = 0f;
                    if (NavMesh.CalculatePath(transform.position, door.transform.position, NavMesh.AllAreas, path))
                        for (int i = 1; i < path.corners.Length; i++)
                            corridorNavDist += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                    graph.AddRoomDoorEdge(currentNodeId, door.gameObject.name,
                        door.elementType == DoorVarcoScript.ElementType.Door,
                        door.transform.position, corridorNavDist, corridorBeyond);
                }
            }
         else
        {
            if (!registeredRoomDoors.ContainsKey(door.gameObject.name))
            {
                registeredRoomDoors[door.gameObject.name] = currentNodeId;

                // Apri fisicamente la porta
                if (!door.IsTraversable)
                    door.TryOpen();

                // Crea il nodo connettore nel grafo se non esiste ancora
                string connId2 = connectorForDoor?.id;
                if (!string.IsNullOrEmpty(connId2) && graph.GetNode(connId2) == null)
                {
                    bool isRev3 = (connectorForDoor.floorFrom == currentFloor);
                    Vector3 connPos3 = isRev3
                        ? connectorForDoor.triggerEndPosition
                        : connectorForDoor.triggerStartPosition;
                    graph.AddRoomNode(connId2, connPos3);
                }

                // Arco currentNodeId→door→connettore, marcato explored (porta aperta = visitata)
                if (!string.IsNullOrEmpty(connId2))
                {
                    float navDist2 = door.NavMeshDistanceTo(transform.position);
                    var edgeConn = graph.AddRoomDoorEdge(currentNodeId, door.gameObject.name,
                        door.elementType == DoorVarcoScript.ElementType.Door,
                        door.transform.position, navDist2, connId2);
                    if (edgeConn != null)
                        graph.MarkEdgeExplored(currentNodeId, edgeConn.id, connId2);
                }

                Debug.Log($"[ExplMgr] Scala '{door.gameObject.name}' aperta da '{currentNodeId}' → '{connId2}' (non entro)");
            }
        }
            return;
        }

        // ── PORTA NORMALE ──
        registeredRoomDoors[door.gameObject.name] = currentNodeId;
        door.discovered = true;

        var node = graph.GetNode(currentNodeId);

        if (node != null && node.type == NodeType.CorridorPoleB)
        {
            string corridorId = currentNodeId.Replace("_B", "");

            if (allCorridorDoors.Contains(door.gameObject.name)) return;

            float dB = Vector3.Distance(door.transform.position, poleBPos);
            float dA = Vector3.Distance(door.transform.position, poleAPos);

            if (dB > 0.6f && dA > 0.6)
            { Debug.Log($"[ExplMgr] {door.gameObject.name} fuori corridoio (dB={dB:F2}) → ignorata"); return; }

            registeredTransitDoors.Add(door.gameObject.name);
            allCorridorDoors.Add(door.gameObject.name);

            string computedSide = ComputeSideRelativeToCorridorAxis(
                door.transform.position, poleAPos, poleBPos);

            graph.AddDoorEdges(corridorId, door.gameObject.name,
                door.elementType == DoorVarcoScript.ElementType.Door,
                door.transform.position, computedSide, dA, dB, orderFW: 0);

            graph.ComputeForwardOrders(corridorId);
            graph.ComputeBackwardOrders(corridorId);
            graph.ComputeExplorationOrder(corridorId);

            Debug.Log($"[ExplMgr] Porta polo-B → {corridorId}: {door.gameObject.name} " +
                      $"dA={dA:F1} dB={dB:F1} side={computedSide}");
            return;
        }

        // Ignora porte che non appartengono fisicamente a questa stanza
        {
            string frontName      = door.roomFront?.name;
            string backName       = door.roomBack?.name;
            bool   doorBelongsHere = (frontName == currentNodeId || backName == currentNodeId);
            if (!doorBelongsHere)
            {
                return;
            }
        }

        string roomBeyond = door.GetRoomNameBeyondDoor(transform.position);
        bool leadsToCorridor = !string.IsNullOrEmpty(roomBeyond)
                            && roomBeyond.ToLower().Contains("corridoio");

        string toNodeId = leadsToCorridor ? roomBeyond : null;
        if (!string.IsNullOrEmpty(toNodeId))
        {
            var targetNode = graph.GetNode(toNodeId);
            if (targetNode != null && targetNode.physicallyVisited && !targetNode.HasUndiscoveredEdges())
                return;
        }

        float navDist = door.NavMeshDistanceTo(transform.position);
        graph.AddRoomDoorEdge(currentNodeId, door.gameObject.name,
            door.elementType == DoorVarcoScript.ElementType.Door,
            door.transform.position, navDist,
            toNodeId: toNodeId);


    }

    // --------------------------------------------------------
    // HANDLER: connettore verticale raggiunto (trigger fisico)
    // --------------------------------------------------------
    private void HandleConnectorReached(string connectorId, int targetFloor, Vector3 pos)
{
    if (state != State.MovingToConnector) return;
    if (pendingConnector == null || pendingConnector.id != connectorId) return;

    bool isReverse = (pendingConnector.floorFrom == targetFloor);
    Vector3 connPosHere = isReverse
        ? pendingConnector.triggerEndPosition
        : pendingConnector.triggerStartPosition;

    int fromFloor = isReverse ? pendingConnector.floorTo : pendingConnector.floorFrom;
    floorGraphs.TryGetValue(fromFloor, out var fromGraph);

    if (fromGraph != null && fromGraph.GetNode(connectorId) == null)
    {
        fromGraph.AddRoomNode(connectorId, connPosHere);
        string sourceId = currentNodeId;
        if (!string.IsNullOrEmpty(sourceId))
        {
            float dist = Vector3.Distance(fromGraph.GetNode(sourceId).position, connPosHere);
            var edge = fromGraph.AddRoomDoorEdge(sourceId, connectorId, true, connPosHere, dist, connectorId);
            if (edge != null) fromGraph.MarkEdgeExplored(sourceId, edge.id, connectorId);
        }
        fromGraph.EnterNode(connectorId);
    }

    bool ascending = targetFloor > currentFloor;
state = State.TraversingStairs;
StartCoroutine(TraverseStairsThen(targetFloor, ascending));
}
    private string FindNearestRoomNodeIn(TopologicalGraph g, Vector3 pos)
    {
        string best = null;
        float bestDist = float.MaxValue;
        foreach (var node in g.AllNodes())
        {
            if (node == null || node.type != NodeType.Room) continue;
            if (node.id == pendingConnector?.id) continue; // salta il connettore stesso
            float d = Vector3.Distance(node.position, pos);
            if (d < bestDist) { bestDist = d; best = node.id; }
        }
        return best;
    }

    // ====================================================
    // STATI
    // ====================================================
    private void UpdateMovingToPole()
    {
        movingToPoleTimer += Time.deltaTime;

        if (movingToPoleTimer >= poleTimeoutSeconds)
        {
            movingToPoleCorridorId = null;
            movingToPoleTimer = 0f;
            ProcessPoleTouched();
            return;
        }

        if (navAgent.pathPending) return;
        if (navAgent.remainingDistance < poleArrivalThreshold)
        {
            movingToPoleCorridorId = null;
            movingToPoleTimer = 0f;
            ProcessPoleTouched();
        }
    }

  private void ProcessPoleTouched()
    {
        var corridorData = FindCorridorData(pendingCorridorId);
        if (corridorData == null) return;

        bool isFirstPole = (state != State.Transiting);

        if (isFirstPole)
        {
            poleAPos = pendingPolePos;
            Vector3 targetB = pendingPoleId == "1"
                ? corridorData.Pole2Position
                : corridorData.Pole1Position;
            poleBPos = targetB;

            graph.AddCorridorPoles(pendingCorridorId, poleAPos, poleBPos,
                Vector3.Distance(poleAPos, poleBPos));

            string comingFromNodeId = currentNodeId;
            string poleAId          = $"{pendingCorridorId}_A";
            currentNodeId           = poleAId;
            graph.EnterNode(poleAId);

            if (!string.IsNullOrEmpty(comingFromNodeId) && 
    (comingFromNodeId.EndsWith("_B") || comingFromNodeId.EndsWith("_A")))
            {
                string comingCorridor = comingFromNodeId.EndsWith("_B") 
                    ? comingFromNodeId.Replace("_B", "") 
                    : comingFromNodeId.Replace("_A", "");

                var linkDoor = !string.IsNullOrEmpty(lastCorridorLinkDoorName)
                    ? FindDoorScript(lastCorridorLinkDoorName) : null;

                string f = linkDoor?.roomFront?.name ?? "";
                string b = linkDoor?.roomBack?.name  ?? "";
                bool directlyLinked = linkDoor != null &&
                                    (f == comingCorridor || b == comingCorridor) &&
                                    (f == pendingCorridorId || b == pendingCorridorId);

                if (directlyLinked)
                {
                    var nodeA = graph.GetNode($"{comingCorridor}_A");
                    var nodeB = graph.GetNode($"{comingCorridor}_B");
                    Vector3 doorPos = linkDoor.transform.position;

                    string fromNode = null;
                    float bestDist = float.MaxValue;
                    if (nodeA != null) { float d = Vector3.Distance(doorPos, nodeA.position); if (d < bestDist) { bestDist = d; fromNode = $"{comingCorridor}_A"; } }
                    if (nodeB != null) { float d = Vector3.Distance(doorPos, nodeB.position); if (d < bestDist) { bestDist = d; fromNode = $"{comingCorridor}_B"; } }

                    string poleBId = $"{pendingCorridorId}_B";
                    float distToNewA = Vector3.Distance(doorPos, poleAPos);
                    float distToNewB = Vector3.Distance(doorPos, poleBPos);
                    string toNode = distToNewA <= distToNewB ? poleAId : poleBId;

                    Vector3 fromPos = graph.GetNode(fromNode)?.position ?? Vector3.zero;
                    Vector3 toPos   = graph.GetNode(toNode)?.position   ?? Vector3.zero;
                    float distFromDoor = NavMeshDist(fromPos, doorPos);
                    float distDoorTo   = NavMeshDist(doorPos, toPos);
                    float dist = (distFromDoor >= 0f && distDoorTo >= 0f)
                        ? distFromDoor + distDoorTo
                        : Vector3.Distance(fromPos, toPos);

                    graph.AddSegmentArc(fromNode, toNode, dist, lastCorridorLinkDoorName, doorPos);
                }
                lastCorridorLinkDoorName = null;
            }
            // Dopo aver creato i poli del nuovo corridoio,
            // cerca corridorLink già registrati che connettono
            // un corridoio già scoperto a questo nuovo corridoio.
            string newCorrA = $"{pendingCorridorId}_A";
            string newCorrB = $"{pendingCorridorId}_B";
            foreach (var door in FindObjectsByType<DoorVarcoScript>(FindObjectsSortMode.None))
            {
                if (!door.isCorridorLink) continue;
                string front = door.roomFront?.name ?? "";
                string back  = door.roomBack?.name  ?? "";

                // La porta deve avere un lato sul nuovo corridoio
                bool newIsA = (front == pendingCorridorId || back == pendingCorridorId);
               /* // L'altro lato deve essere esattamente un corridoio con poli già scoperti,
                // non una stanza intermedia
                var otherCorrDataCheck = FindCorridorData(otherCorrId);
                if (otherCorrDataCheck == null) continue; // non è un corridoio, salta*/
                if (!newIsA) continue;

                // L'altro lato deve essere un corridoio già scoperto
                string otherCorrId = (front == pendingCorridorId) ? back : front;
                // ← aggiungi qui, dopo otherCorrId è dichiarato
                var otherCorrDataCheck = FindCorridorData(otherCorrId);
                if (otherCorrDataCheck == null) continue;
                var otherNodeA = graph.GetNode($"{otherCorrId}_A");
                var otherNodeB = graph.GetNode($"{otherCorrId}_B");
                if (otherNodeA == null && otherNodeB == null) continue; // non ancora scoperto

                // Trova il polo più vicino dell'altro corridoio rispetto alla porta
                Vector3 doorPos = door.transform.position;
                string fromNode = null;
                float bestDist = float.MaxValue;
                if (otherNodeA != null)
                {
                    float d = Vector3.Distance(doorPos, otherNodeA.position);
                    if (d < bestDist) { bestDist = d; fromNode = $"{otherCorrId}_A"; }
                }
                if (otherNodeB != null)
                {
                    float d = Vector3.Distance(doorPos, otherNodeB.position);
                    if (d < bestDist) { bestDist = d; fromNode = $"{otherCorrId}_B"; }
                }

                // Trova il polo più vicino del nuovo corridoio rispetto alla porta
                float distToNewA = Vector3.Distance(doorPos, poleAPos);
                float distToNewB = Vector3.Distance(doorPos, poleBPos);
                string toNode = distToNewA <= distToNewB ? newCorrA : newCorrB;
        
                Vector3 fromPos = graph.GetNode(fromNode)?.position ?? Vector3.zero;
                Vector3 toPos   = graph.GetNode(toNode)?.position   ?? Vector3.zero;
                float distFromDoor = NavMeshDist(fromPos, doorPos);
                float distDoorTo   = NavMeshDist(doorPos, toPos);
                float dist = (distFromDoor >= 0f && distDoorTo >= 0f)
                    ? distFromDoor + distDoorTo
                    : Vector3.Distance(fromPos, toPos);

                graph.AddSegmentArc(fromNode, toNode, dist, door.gameObject.name, doorPos);
               
            }

            transitCorridorId = pendingCorridorId;
            registeredTransitDoors.Clear();

            navAgent.isStopped = false;
            navAgent.SetDestination(targetB);
            explorationVisionCone?.StartTransitScan(poleAPos, targetB, pendingCorridorId);
            inRoomMode = false;
            state      = State.Transiting;

            Debug.Log($"[ExplMgr] Transito {pendingCorridorId}: A={poleAPos} → B={targetB}");
        }
        else
        {
            explorationVisionCone?.StopTransitScan();

            string poleBId = $"{pendingCorridorId}_B";
            currentNodeId  = poleBId;
            graph.EnterNode(poleBId);

            ScanCorridorDoorsFromScript(pendingCorridorId);

            graph.ComputeForwardOrders(pendingCorridorId);
            graph.ComputeBackwardOrders(pendingCorridorId);
            graph.ComputeExplorationOrder(pendingCorridorId);

            graph.MarkEdgeExplored($"{pendingCorridorId}_A",
                $"central_{pendingCorridorId}", poleBId);

            movingToPoleCorridorId = null;
            Debug.Log($"[ExplMgr] Polo B toccato. Inizio ispezione {pendingCorridorId}.");
            StartRotation360(isRoom: false);
        }

        pendingCorridorId = null;
        pendingPoleId     = null;
    }

    private void UpdateTransiting()
    {
        transitTimer += Time.deltaTime;

        if (navAgent.pathPending && transitTimer < poleTimeoutSeconds) return;

        if (navAgent.remainingDistance < poleArrivalThreshold || transitTimer >= poleTimeoutSeconds)
        {
            transitTimer = 0f;
            pendingCorridorId = transitCorridorId;
            pendingPoleId     = "B_fallback";
            pendingPolePos    = poleBPos;
            ProcessPoleTouched();
        }
    }

    private void StartRotation360(bool isRoom)
    {
        inRoomMode    = isRoom;
        rotationAccum = 0f;
        navAgent.isStopped = true;
        state = State.Rotating360;

        if (isRoom)
        registeredRoomDoors.Clear();

        // Marca il nodo come fisicamente visitato (360° reale).
        // Questo distingue i nodi aggiunti programmaticamente (es. ExecuteFloorChange)
        // dai nodi realmente esplorati, impedendo che LOOP EVITATO li salti.
        if (isRoom && graph != null)
        {
            var visitNode = graph.GetNode(currentNodeId);
            if (visitNode != null) visitNode.physicallyVisited = true;
        }

    }

    private void UpdateRotating360()
    {
        navAgent.isStopped = true;
        float step = rotationSpeed * Time.deltaTime;
        transform.Rotate(0, step, 0);
        rotationAccum += step;

        if (rotationAccum >= 360f)
        {
            navAgent.isStopped = false;
            rotationAccum = 0f;
            Debug.Log($"[ExplMgr] 360° completato in '{currentNodeId}'.");

            if (currentNodeId != null && currentNodeId.EndsWith("_B"))
            {
                string corrId = currentNodeId.Replace("_B", "");
                graph.ComputeForwardOrders(corrId);
                graph.ComputeBackwardOrders(corrId);
                graph.ComputeExplorationOrder(corrId);
            }

            if (inRoomMode && !string.IsNullOrEmpty(currentNodeId))
                {
                    var currentNode = graph.GetNode(currentNodeId);
                    bool hasUnexploredDoors = currentNode != null &&
                        currentNode.edges.Exists(e => e != null &&
                                                    e.state != EdgeState.Explored &&
                                                    e.edgeType == EdgeType.Door);
                    if (!hasUnexploredDoors)
                        ScanRoomDoorsFromScript(currentNodeId);
                }

            DecideNextAction();
        }
    }

    private void UpdateMovingToDoor()
    {
        if (navAgent.pathPending) return;
        if (navAgent.remainingDistance <= inspectionDistance)
        {
            navAgent.isStopped = true;
            state              = State.InspectingDoor;
        }
    }

    private void UpdateInspectingDoor()
    {
        if (currentEdge == null) { DecideNextAction(); return; }

        if (currentEdge.doorState == DoorState.Locked)
        {
            graph.MarkEdgeExplored(currentNodeId, currentEdge.id, null);
            currentEdge = null;
            navAgent.isStopped = false;
            DecideNextAction();
            return;
        }

        var door = FindDoorScript(currentEdge.id);
        if (door != null && !door.IsTraversable)
        {
            if (!door.TryOpen())
            {
                currentEdge.doorState = DoorState.Locked;
                graph.MarkEdgeExplored(currentNodeId, currentEdge.id, null);
                currentEdge = null;
                navAgent.isStopped = false;
                DecideNextAction();
                return;
            }
        }

        EnterThroughEdge(currentEdge);
    }

    private float enteringRoomTimer   = 0f;
    private float enteringRoomTimeout = 3f;

    private void UpdateEnteringRoom()
    {
        if (navAgent.pathPending) return;
        enteringRoomTimer += Time.deltaTime;

      if (navAgent.remainingDistance < arrivalThreshold && !navAgent.pathPending || enteringRoomTimer >= enteringRoomTimeout)
        {
            enteringRoomTimer = 0f;
            StartRotation360(isRoom: true);
        }
    }

    private void UpdateInsideRoom() => StartRotation360(isRoom: true);

    private void UpdateBacktracking()
    {
        if (navAgent.pathPending) return;

        backtrackTimer += Time.deltaTime;
        bool arrived    = navAgent.hasPath && navAgent.remainingDistance <= arrivalThreshold;
        bool pathFailed = !navAgent.hasPath;
        bool timedOut   = backtrackTimer >= backtrackTimeout;

        if (arrived || pathFailed || timedOut)
        {
            backtrackTimer = 0f;
            if (timedOut && !arrived){}

            currentNodeId = graph.CurrentNodeId;
            if (string.IsNullOrEmpty(currentNodeId)) { CheckFloorCompletion(); return; }

            Debug.Log($"[ExplMgr] Backtrack completato in {currentNodeId}. Prossima azione!");
            state = State.Idle;
            DecideNextAction();
        }
    }

private void UpdateMovingToConnector()
{
    if (navAgent.pathPending) return;
    if (pendingConnector != null &&
        navAgent.remainingDistance <= arrivalThreshold * 2f)
    {
        int targetFloor = (pendingConnector.floorFrom == currentFloor)
            ? pendingConnector.floorTo
            : pendingConnector.floorFrom;
        bool ascending = targetFloor > currentFloor;

        state = State.TraversingStairs;
        StartCoroutine(TraverseStairsThen(targetFloor, ascending));
    }
}
    // ====================================================
    // DECISIONE PROSSIMA AZIONE
    // ====================================================
    private void DoBacktrack()
{
    string    targetNode = null;
    GraphNode targetData = null;

    while (true)
    {
        targetNode = graph.Backtrack();
        if (targetNode == null) { CheckFloorCompletion(); return; }

        targetData = graph.GetNode(targetNode);
        if (targetData != null && targetData.HasUndiscoveredEdges())
        {
            if (targetNode.EndsWith("_A") || targetNode.EndsWith("_B"))
            {
                string corridorId = targetNode.Substring(0, targetNode.Length - 2);
                var nodeA = graph.GetNode($"{corridorId}_A");
                var nodeB = graph.GetNode($"{corridorId}_B");
                bool corridorDone = nodeA != null && nodeA.physicallyVisited
                                && nodeB != null && nodeB.physicallyVisited
                                && graph.IsCorridorFullyTransited(corridorId);
                if (corridorDone)
                {
                    continue;
                }
            }
            break;
        }
        Debug.Log($"[Backtrack] {targetNode} già esplorato, riavvolgo...");
    }

    // Se il backtrack target è un polo corridoio, salta il viaggio fisico
    // e vai direttamente alla porta successiva
    if (targetNode.EndsWith("_A") || targetNode.EndsWith("_B"))
    {
        backtrackTimer = 0f;
        currentNodeId  = targetNode;
        Debug.Log($"[ExplMgr] Backtrack diretto a '{targetNode}' — salto viaggio fisico.");
        state = State.Idle;
        DecideNextAction();
        return;
    }

    backtrackTimer = 0f;
    navAgent.isStopped = false;
    navAgent.SetDestination(targetData.position);
    state = State.Backtracking;
}

    private void DecideNextAction()
    {
        if (string.IsNullOrEmpty(currentNodeId)) return;

        var node = graph.GetNode(currentNodeId);
        if (node == null) { DoBacktrack(); return; }

        GraphEdge nextEdge = null;

        if (node.type == NodeType.CorridorPoleB)
        {
            nextEdge = graph.NextCorridorEdgeToInspect(currentNodeId);
            if (nextEdge == null)
            {
                nextEdge = graph.NextRoomEdgeToInspect(currentNodeId);
                if (nextEdge != null){}
            }
             if (nextEdge == null)
                {
                    nextEdge = node.edges.FirstOrDefault(e =>
                        e != null &&
                        e.state != EdgeState.Explored &&
                        e.isCorridorLink);
                    if (nextEdge != null)
                        Debug.Log($"[ExplMgr] Corridor link non esplorato trovato: {nextEdge.id}");
                }
        }
        else if (node.type == NodeType.Room)
            nextEdge = graph.NextRoomEdgeToInspect(currentNodeId);
        else
            nextEdge = graph.NextEdgeToInspect(currentNodeId);

        if (nextEdge != null)
        {
            currentEdge        = nextEdge;
            navAgent.isStopped = false;
            navAgent.SetDestination(nextEdge.position);
            state = State.MovingToDoor;
            Debug.Log($"[ExplMgr] Prossima porta: {nextEdge.id} navDist={nextEdge.navMeshDist:F1}");
        }
        else
        {
            DoBacktrack();
        }
    }

    // ====================================================
    // ENTRA ATTRAVERSO UN ARCO
    // ====================================================
    private void EnterThroughEdge(GraphEdge edge)
{
    var doorScript = FindDoorScript(edge.id);

   string newNodeId;
if (doorScript != null && (edge.id.StartsWith("bw_") || edge.id.StartsWith("fw_")))
{
    string corridorId = (currentNodeId.EndsWith("_A") || currentNodeId.EndsWith("_B"))
        ? currentNodeId.Substring(0, currentNodeId.Length - 2)
        : currentNodeId;
    string front = doorScript.roomFront?.name ?? "";
    string back  = doorScript.roomBack?.name  ?? "";
    if (front == corridorId) newNodeId = back;
    else if (back == corridorId) newNodeId = front;
    else newNodeId = doorScript.GetRoomNameBeyondDoor(transform.position);
}
else if (doorScript != null)
    newNodeId = doorScript.GetRoomNameBeyondDoor(transform.position);
else
    newNodeId = edge.id;

    if (doorScript != null && IsConnectorDoor(doorScript.gameObject.name))
    {
        var connector = FindConnectorForDoor(doorScript.gameObject.name);
        if (connector != null) newNodeId = connector.id;

        if (newNodeId == currentNodeId)
        {
            string beyond = doorScript.GetRoomNameBeyondDoor(transform.position);
            if (!string.IsNullOrEmpty(beyond) && beyond != currentNodeId)
                newNodeId = beyond;
        }

        if (graph.GetNode(newNodeId) == null)
            graph.AddRoomNode(newNodeId, edge.position);

        graph.MarkEdgeExplored(currentNodeId, edge.id, newNodeId);
        SyncTwinCorridorEdge(edge.id, newNodeId);

        currentEdge = null;
        navAgent.isStopped = false;
        DecideNextAction();
        return;
    }

    bool isTargetCorridor = FindCorridorData(newNodeId) != null;

    var existingNode = graph.GetNode(newNodeId);
    if (existingNode != null && !existingNode.HasUndiscoveredEdges() && !isTargetCorridor && existingNode.physicallyVisited)
    {
        Debug.LogWarning($"[ExplMgr] LOOP EVITATO: '{newNodeId}' già completata.");
        graph.MarkEdgeExplored(currentNodeId, edge.id, newNodeId);
        SyncTwinCorridorEdge(edge.id, newNodeId);
        currentEdge = null;
        navAgent.isStopped = false;
        DecideNextAction();
        return;
    }

    graph.MarkEdgeExplored(currentNodeId, edge.id, isTargetCorridor ? null : newNodeId);
    SyncTwinCorridorEdge(edge.id, newNodeId);

    if (isTargetCorridor)
    {
        var currentNode = graph.GetNode(currentNodeId);
        if (currentNode != null)
        {
            currentNode.edges.RemoveAll(e =>
                e != null &&
                e.edgeType == EdgeType.Door &&
                e.toNodeId == edge.toNodeId);
        }

        if (graph.IsCorridorFullyTransited(newNodeId))
        {
            Debug.LogWarning($"[ExplMgr] Corridoio '{newNodeId}' già transitato.");
            currentEdge = null;
            navAgent.isStopped = false;
            DecideNextAction();
            return;
        }
        // Controlla anche se corridoio già fisicamente visitato e senza porte pendenti
        var targetCorridorNode = graph.GetNode($"{newNodeId}_B");
        if (targetCorridorNode != null && targetCorridorNode.physicallyVisited && !targetCorridorNode.HasUndiscoveredEdges())
        {
            Debug.LogWarning($"[ExplMgr] Corridoio '{newNodeId}' già completato, salto transito.");
            currentEdge = null;
            navAgent.isStopped = false;
            DecideNextAction();
            return;
        }
        idleTargetCorridorId = newNodeId;
        state = State.Idle;
    }
    else
    {
       var floorData2 = buildingGraph?.GetFloor(currentFloor);
        var roomObj2   = floorData2?.roomObjects.Find(r => r != null && r.name == newNodeId);
        Vector3 roomCenter = roomObj2 != null ? GetRoomCenter(roomObj2) : edge.position;
        graph.AddRoomNode(newNodeId, roomCenter);
        currentNodeId = newNodeId;
        graph.EnterNode(currentNodeId);
        state = State.EnteringRoom;
    }

    // Calcolo target di navigazione
    Vector3 enterTarget;
    if (isTargetCorridor)
    {
        // Corridoio: comportamento originale — entra oltre la porta
        Vector3 doorNormal = doorScript != null ? doorScript.transform.forward : transform.forward;
        Vector3 toDoor     = (edge.position - transform.position).normalized;
        if (Vector3.Dot(doorNormal, toDoor) < 0) doorNormal = -doorNormal;
        enterTarget = edge.position + doorNormal * enterOffset;
    }
    else
    {
        // Stanza: vai al centro del GameObject della stanza
        var floorData = buildingGraph?.GetFloor(currentFloor);
        var roomObj   = floorData?.roomObjects.Find(r => r != null && r.name == newNodeId);
        if (roomObj != null)
            enterTarget = GetRoomCenter(roomObj);
        else
        {
            // Fallback: offset oltre la porta
            Vector3 doorNormal = doorScript != null ? doorScript.transform.forward : transform.forward;
            Vector3 toDoor     = (edge.position - transform.position).normalized;
            if (Vector3.Dot(doorNormal, toDoor) < 0) doorNormal = -doorNormal;
            enterTarget = edge.position + doorNormal * enterOffset;
        }
    }

    NavMeshHit hit;
    if (!NavMesh.SamplePosition(enterTarget, out hit, 2f, NavMesh.AllAreas))
    {
        enterTarget = edge.position;
        if (!NavMesh.SamplePosition(enterTarget, out hit, 2f, NavMesh.AllAreas))
        {
            hit.position = edge.position;
            Debug.LogWarning($"[ExplMgr] NavMesh non trovato vicino a '{newNodeId}'.");
        }
    }
    enterTarget = hit.position;

    navAgent.isStopped = false;
    navAgent.SetDestination(enterTarget);

    if (isTargetCorridor)
        lastCorridorLinkDoorName = (edge.id.StartsWith("bw_") || edge.id.StartsWith("fw_"))
            ? edge.id.Substring(3) : edge.id;
    currentEdge = null;
}

    private void SyncTwinCorridorEdge(string edgeId, string toNodeId)
    {
        if (string.IsNullOrEmpty(currentNodeId)) return;

        if (currentNodeId.EndsWith("_B") && edgeId.StartsWith("bw_"))
        {
            string poleA  = currentNodeId.Replace("_B", "_A");
            string fwEdge = edgeId.Replace("bw_", "fw_");
            graph.MarkEdgeExplored(poleA, fwEdge, toNodeId);
        }
        else if (currentNodeId.EndsWith("_A") && edgeId.StartsWith("fw_"))
        {
            string poleB  = currentNodeId.Replace("_A", "_B");
            string bwEdge = edgeId.Replace("fw_", "bw_");
            graph.MarkEdgeExplored(poleB, bwEdge, toNodeId);
        }
    }

    // ====================================================
    // COMPLETAMENTO PIANO
    // ====================================================
    private void CheckFloorCompletion()
{
    if (buildingGraph == null) { OnAllFloorsCompleted(); return; }

    var floorData = buildingGraph.GetFloor(currentFloor);

    if (!IsCurrentFloorFullyExplored(floorData))
    {
        NavigateToUnexploredCorridor(floorData);
        return;
    }

    if (floorData != null) floorData.explored = true;
    Debug.Log($"[ExplMgr] Piano {currentFloor} completato.");

    var unexplored = buildingGraph.GetUnexploredFloors();
    if (unexplored.Count == 0)
    {
        Debug.Log("[ExplMgr] Tutti i piani esplorati.");
        OnAllFloorsCompleted();
        return;
    }

    // STEP 1 — connettore diretto dal piano corrente a un piano non esplorato
    // Ordina per vicinanza al piano corrente (evita di scendere quando si può salire)
    unexplored.Sort((a, b) =>
        Mathf.Abs(a.floorIndex - currentFloor)
            .CompareTo(Mathf.Abs(b.floorIndex - currentFloor)));

    foreach (var targetFloor in unexplored)
    {
        var connectors = buildingGraph.GetConnectors(currentFloor, targetFloor.floorIndex);
        connectors.RemoveAll(c => traversedConnectors.Contains(c.id));
        if (connectors.Count == 0) continue;

        connectors.Sort((a, b) => a.costUp.CompareTo(b.costUp));
        pendingConnector = connectors[0];

        bool isReverse = (pendingConnector.floorFrom != currentFloor);
        Vector3 navTarget = isReverse
            ? pendingConnector.triggerEndPosition
            : pendingConnector.triggerStartPosition;

        Debug.Log($"[ExplMgr] Piano {currentFloor} → piano {targetFloor.floorIndex} " +
                  $"via '{pendingConnector.id}' (diretto)");
        navAgent.isStopped = false;
        navAgent.SetDestination(navTarget);
        state = State.MovingToConnector;
        return;
    }

    // STEP 2 — nessun connettore diretto: cerca un piano intermedio già esplorato
    // che abbia un connettore verso un piano non ancora esplorato
    Debug.Log($"[ExplMgr] Nessun connettore diretto da piano {currentFloor}. " +
              $"Cerco percorso indiretto...");

    unexplored.Sort((a, b) => a.floorIndex.CompareTo(b.floorIndex));

    // Piani già visitati, ordinati per vicinanza al piano corrente
    var visitedFloors = new List<int>(floorGraphs.Keys);
    visitedFloors.Sort((a, b) =>
        Mathf.Abs(a - currentFloor).CompareTo(Mathf.Abs(b - currentFloor)));

    foreach (var targetFloor in unexplored)
    {
        foreach (int midFloor in visitedFloors)
        {
            if (midFloor == currentFloor) continue;

            // Il piano intermedio deve avere un connettore non traversato verso targetFloor
            var connToTarget = buildingGraph.GetConnectors(midFloor, targetFloor.floorIndex);
            connToTarget.RemoveAll(c => traversedConnectors.Contains(c.id));
            if (connToTarget.Count == 0) continue;

            // Cerca il connettore tra currentFloor e midFloor
            // (può essere già traversato — vogliamo ri-attraversarlo)
            var connToMid = buildingGraph.GetConnectors(currentFloor, midFloor);
            if (connToMid.Count == 0) continue;

            connToMid.Sort((a, b) => a.costUp.CompareTo(b.costUp));
            pendingConnector = connToMid[0];

            bool isReverse = (pendingConnector.floorFrom != currentFloor);
            Vector3 navTarget = isReverse
                ? pendingConnector.triggerEndPosition
                : pendingConnector.triggerStartPosition;

            Debug.Log($"[ExplMgr] Percorso indiretto: piano {currentFloor} → piano {midFloor} " +
                      $"→ piano {targetFloor.floorIndex} via '{pendingConnector.id}'");
            navAgent.isStopped = false;
            navAgent.SetDestination(navTarget);
            state = State.MovingToConnector;
            return;
        }
    }

    Debug.Log("[ExplMgr] Tutti i piani esplorati (o non raggiungibili).");
    OnAllFloorsCompleted();
}
    

 // ──────────────────────────────────────────────────────────────
// METODO 2: IsCurrentFloorFullyExplored
// ──────────────────────────────────────────────────────────────
private bool IsCurrentFloorFullyExplored(FloorNode floorData)
{
    // 1. Il grafo del piano corrente deve avere tutti gli archi esplorati
    if (!graph.IsFullyExplored()) return false;
 
    // 2. Tutti i roomObjects del BuildingGraph per questo piano devono
    //    essere stati visitati come nodi nel grafo.
    if (floorData != null && floorData.roomObjects.Count > 0)
    {
        foreach (string roomId in floorData.roomIds)
        {
            // Check diretto: nodo con ID uguale al nome del GameObject
            if (graph.GetNode(roomId) != null) continue;
 
            // ── FIX BUG 2: i corridoi nel grafo si chiamano {id}_A e {id}_B ──
            // Un corridoio nel BuildingGraph si chiama es. "corridoio1",
            // ma nel TopologicalGraph esiste come "corridoio1_A" e "corridoio1_B".
            // Se almeno il polo A è presente, il corridoio è stato attraversato.
           var poleANode = graph.GetCorridorPoleNode($"{roomId}_A");
            if (poleANode != null)
            {
                var centralEdge = poleANode.edges?.Find(e => e?.id == $"central_{roomId}");
                if (centralEdge != null && centralEdge.state == EdgeState.Explored)
                    continue;

                // FIX: considera completato anche se entrambi i poli sono fisicamente visitati
                var poleBNode = graph.GetNode($"{roomId}_B");
                if (poleANode.physicallyVisited && poleBNode != null && poleBNode.physicallyVisited)
                {
                    Debug.Log($"[ExplMgr] '{roomId}' — entrambi i poli visitati, considero completato.");
                    continue;
                }

                Debug.Log($"[ExplMgr] Piano {currentFloor}: '{roomId}' ha polo ma transito non completato.");
                return false;
            }
            
            return false;
        }
    }
 
    return true;
}
    private Vector3 GetRoomCenter(GameObject roomObj)
{
    if (roomObj == null) return Vector3.zero;

    var col = roomObj.GetComponentInChildren<Collider>();
    if (col != null) return col.bounds.center;

    var rend = roomObj.GetComponentInChildren<Renderer>();
    if (rend != null) return rend.bounds.center;

    return roomObj.transform.position;
}

    private void NavigateToUnexploredCorridor(FloorNode floorData)
    {
        if (floorData != null)
        {
            foreach (string roomId in floorData.roomIds)
            {
                if (graph.GetNode(roomId) != null) continue;
                // Skip se la porta fisica verso questa stanza è già stata esplorata (anche con nome nodo diverso)
                string doorCheck = FindDoorBetweenNodes(currentNodeId, roomId);
                if (!string.IsNullOrEmpty(doorCheck))
                {
                    var currNode = graph.GetNode(currentNodeId);
                    if (currNode != null && currNode.edges.Exists(e =>
                        e?.id == doorCheck && e.state == EdgeState.Explored))
                        continue;
                }
                var poleANodeNav = graph.GetCorridorPoleNode($"{roomId}_A");
                if (poleANodeNav != null)
                {
                    var centralEdgeNav = poleANodeNav.edges?.Find(e => e?.id == $"central_{roomId}");
                    if (centralEdgeNav != null && centralEdgeNav.state == EdgeState.Explored)
                        continue; // davvero già transitato
                    // altrimenti cade giù e NavigateToUnexploredCorridor lo naviga normalmente
                }

                var roomObj = floorData.roomObjects.Find(r => r != null && r.name == roomId);
                if (roomObj == null) continue;

                // FIX Bug 2: se è un corridoio, usa MovingToPole invece di EnteringRoom
                var corridorData = FindCorridorData(roomId);
                if (corridorData != null)
                {
                    float d1 = Vector3.Distance(transform.position, corridorData.Pole1Position);
                    float d2 = Vector3.Distance(transform.position, corridorData.Pole2Position);
                    pendingCorridorId      = roomId;
                    pendingPoleId          = d1 <= d2 ? "1" : "2";
                    pendingPolePos         = d1 <= d2 ? corridorData.Pole1Position : corridorData.Pole2Position;
                    movingToPoleCorridorId = roomId;
                    movingToPoleTimer      = 0f;
                    navAgent.isStopped     = false;
                    navAgent.SetDestination(pendingPolePos);
                    state = State.MovingToPole;
                    return;
                }

                Vector3 center = GetRoomCenter(roomObj);
                graph.AddRoomNode(roomId,center);
                graph.EnterNode(roomId);
                var prevNode = graph.GetNode(currentNodeId);
                bool alreadyLinked = prevNode != null && prevNode.edges.Exists(e => e != null && e.toNodeId == roomId);
                if (!alreadyLinked && prevNode != null)
                {
                    float dist = Vector3.Distance(prevNode.position, center);
                // Trova la porta fisica più vicina al nodo connettore corrente
                string synDoorName = FindDoorBetweenNodes(currentNodeId, roomId);
                if (string.IsNullOrEmpty(synDoorName))
                    synDoorName = $"nav_{currentNodeId}_{roomId}";

                var synEdge = graph.AddRoomDoorEdge(currentNodeId, synDoorName,
                    false, center, dist, roomId);
                if (synEdge != null)
                    graph.MarkEdgeExplored(currentNodeId, synEdge.id, roomId);                }
                foreach (var node in graph.AllNodes())
                {
                    if (node == null) continue;
                    foreach (var edge in node.edges)
                    {
                        if (edge != null && edge.toNodeId == node.id && edge.state == EdgeState.Explored)
                        {
                            edge.toNodeId = roomId;
                            Debug.Log($"[ExplMgr] Arco {node.id}→{edge.id} corretto: toNodeId aggiornato a '{roomId}'");
                        }
                    }
                }
                currentNodeId = roomId;
                navAgent.isStopped = false;
                navAgent.SetDestination(center);
                state = State.EnteringRoom;
                return;
            }
        }

        string targetId = graph.FindNearestNodeWithDiscoveredEdges(transform.position);
        if (targetId == null) { OnAllFloorsCompleted(); return; }

        navAgent.SetDestination(graph.GetNode(targetId).position);
        currentNodeId = targetId;
        state = State.Backtracking;
    }

    private void ExecuteFloorChange(int targetFloor)
{
    if (state == State.Completed) return;

    int fromFloor = currentFloor;
    string connNodeId = string.Empty;
    var activeConnector = pendingConnector;
    bool isReverse = (activeConnector != null && activeConnector.floorFrom == targetFloor);

    if (activeConnector != null)
    {
        traversedConnectors.Add(activeConnector.id);
        connNodeId = activeConnector.id;

        Vector3 connPosCurrentFloor = isReverse
            ? activeConnector.triggerEndPosition
            : activeConnector.triggerStartPosition;

        Vector3 connPosDestFloor = isReverse
            ? activeConnector.triggerStartPosition
            : activeConnector.triggerEndPosition;

        if (graph != null && graph.GetNode(connNodeId) == null)
            graph.AddRoomNode(connNodeId, connPosCurrentFloor);

        if (!floorGraphs.ContainsKey(targetFloor))
            floorGraphs[targetFloor] = new TopologicalGraph();

        var destGraph = floorGraphs[targetFloor];
        if (destGraph.GetNode(connNodeId) == null)
            destGraph.AddRoomNode(connNodeId, connPosDestFloor);
        
         bool alreadyExists = ConnectorLinks.Exists(l =>
            l.connectorId == activeConnector.id &&
            ((l.floorFrom == fromFloor   && l.floorTo == targetFloor) ||
             (l.floorFrom == targetFloor && l.floorTo == fromFloor)));

        if (!alreadyExists)
        {

        ConnectorLinks.Add(new FloorConnectorLink
        {
            connectorId = activeConnector.id,
            floorFrom   = fromFloor,
            nodeIdFrom  = connNodeId,
            posFrom     = connPosCurrentFloor,
            floorTo     = targetFloor,
            nodeIdTo    = connNodeId,
            posTo       = connPosDestFloor,
             bidirectional = activeConnector.bidirectional 
        });
        }

    }
    else if (!floorGraphs.ContainsKey(targetFloor))
    {
        floorGraphs[targetFloor] = new TopologicalGraph();
    }

    currentFloor     = targetFloor;
    pendingConnector = null;
    

    registeredRoomDoors.Clear();
    registeredTransitDoors.Clear();
    allCorridorDoors.Clear();

    var floorData = buildingGraph?.GetFloor(targetFloor);
    if (floorData != null)
    {
        Vector3 warpTarget = (activeConnector != null)
            ? (isReverse ? activeConnector.triggerStartPosition : activeConnector.triggerEndPosition)
            : floorData.spawnPosition;

        
        string spawnId = !string.IsNullOrEmpty(connNodeId)
            ? connNodeId
            : (floorData.spawnObject != null ? floorData.spawnObject.name : $"floor_{targetFloor}_entry");

        currentNodeId = spawnId;
        if (graph.GetNode(currentNodeId) == null)
            graph.AddRoomNode(currentNodeId, floorData.spawnPosition);

        graph.EnterNode(currentNodeId);
        var fd = buildingGraph?.GetFloor(targetFloor);
        if (fd != null && IsCurrentFloorFullyExplored(fd))
        {
            CheckFloorCompletion();
            return;
        }
        if (!string.IsNullOrEmpty(connNodeId))
    {
        string adjRoom    = null;
        string adjDoorName = null;
        Vector3 adjPos    = Vector3.zero;
        CorridorData exitCorridor = FindCorridorContainingPosition(navAgent.transform.position, 3f);
        if (exitCorridor != null)
    {
        pendingArrivalDoor     = null;
        pendingCorridorId      = exitCorridor.corridorId;
        pendingPoleId          = "1";
        pendingPolePos         = exitCorridor.Pole1Position;
        movingToPoleCorridorId = exitCorridor.corridorId;
        movingToPoleTimer      = 0f;
        navAgent.isStopped     = false;
        navAgent.SetDestination(exitCorridor.Pole1Position);
        state = State.MovingToPole;
        return;  // ← salta Tentativo 1/2 e il blocco corridoio finale
    }

        // Tentativo 1: usa la porta di arrivo fisica
        if (pendingArrivalDoor != null)
        {
            var arrDoor = pendingArrivalDoor;
            string front = arrDoor.roomFront?.name;
            string back  = arrDoor.roomBack?.name;
            string candidate = (front == connNodeId) ? back
                            : (back  == connNodeId) ? front
                            : string.IsNullOrEmpty(front) ? back : front;

            var fdAdj = buildingGraph?.GetFloor(targetFloor);
            if (!string.IsNullOrEmpty(candidate) && candidate != connNodeId
                && (fdAdj?.roomIds.Contains(candidate) ?? false))
            {
                adjRoom     = candidate;
                adjDoorName = arrDoor.gameObject.name;
                var rObj    = fdAdj.roomObjects.Find(r => r != null && r.name == adjRoom);
                adjPos      = rObj != null ? GetRoomCenter(rObj) : arrDoor.transform.position;
            }
        }
        pendingArrivalDoor = null;

        // Tentativo 2: fallback tramite FindDoorBetweenNodes
        if (string.IsNullOrEmpty(adjRoom))
        {
            var fdAdj = buildingGraph?.GetFloor(targetFloor);
            if (fdAdj != null)
            {
                foreach (string rid in fdAdj.roomIds)
                {
                    string dName = FindDoorBetweenNodes(connNodeId, rid);
                    if (string.IsNullOrEmpty(dName)) continue;
                    var rObj = fdAdj.roomObjects.Find(r => r != null && r.name == rid);
                    if (rObj == null) continue;
                    adjRoom     = rid;
                    adjDoorName = dName;
                    adjPos      = GetRoomCenter(rObj);
                    break;
                }
            }
        }

     if (!string.IsNullOrEmpty(adjRoom))
{
    // Se adjRoom è un corridoio con poli, non trattarlo come stanza normale
    var adjCorridor = FindCorridorData(adjRoom);
    if (adjCorridor != null)
    {
        pendingCorridorId      = adjRoom;
        pendingPoleId          = "1";
        pendingPolePos         = adjCorridor.Pole1Position;
        movingToPoleCorridorId = adjRoom;
        movingToPoleTimer      = 0f;
        navAgent.isStopped     = false;
        navAgent.SetDestination(adjCorridor.Pole1Position);
        state = State.MovingToPole;
        Debug.Log($"[ExplMgr] Destinazione è corridoio '{adjRoom}' → vado al polo.");
        return;
    }

    // adjRoom è una stanza normale, gestione originale
    if (graph.GetNode(adjRoom) == null)
        graph.AddRoomNode(adjRoom, adjPos);

    float dAdj = Vector3.Distance(graph.GetNode(connNodeId).position, adjPos);
    var adjEdge = graph.AddRoomDoorEdge(connNodeId, adjDoorName,
        false, adjPos, dAdj, adjRoom);
    if (adjEdge != null)
        graph.MarkEdgeExplored(connNodeId, adjEdge.id, adjRoom);

    graph.EnterNode(adjRoom);
    currentNodeId = adjRoom;

    inRoomMode             = false;
    movingToPoleCorridorId = null;
    idleTargetCorridorId   = null;

    navAgent.isStopped = false;
    navAgent.SetDestination(adjPos);
    enteringRoomTimer   = 0f;
    enteringRoomTimeout = 6f;
    state               = State.EnteringRoom;

    Debug.Log($"[ExplMgr] Cammino verso '{adjRoom}' via '{adjDoorName}'.");
    return;
}
    }
        // NUOVO: pre-collega le stanze già note del piano destinazione al nodo connettore
        if (!string.IsNullOrEmpty(connNodeId))
        {
            var fd2 = buildingGraph?.GetFloor(targetFloor);
            if (fd2 != null)
            {
                foreach (string rid in fd2.roomIds)
                {
                    // cerca la porta fisica tra connettore e stanza
                    string dName = FindDoorBetweenNodes(connNodeId, rid);
                    if (string.IsNullOrEmpty(dName)) continue;
                        bool isConnTrigger = buildingGraph.connectors?.Any(c =>
                        (c.triggerStartObject != null && c.triggerStartObject.name == dName) ||
                        (c.triggerEndObject   != null && c.triggerEndObject.name   == dName)) ?? false;
                    if (isConnTrigger) continue;
                    var existingEdge = graph.GetNode(connNodeId)?.edges
                        .Find(e => e?.toNodeId == rid || e?.id == dName);
                    if (existingEdge != null) continue;
                    var roomObj = fd2.roomObjects.Find(r => r != null && r.name == rid);
                    if (roomObj == null) continue;
                    Vector3 rCenter = GetRoomCenter(roomObj);
                    float d =Vector3.Distance(graph.GetNode(connNodeId).position, rCenter);
                    graph.AddRoomDoorEdge(connNodeId, dName, false, rCenter, d, rid);
                }
            }
        }

        // NON si crea un arco segment spawn→connettore.
    }

    inRoomMode             = false;
    movingToPoleCorridorId = null;
    idleTargetCorridorId   = null;

    CorridorData spawnCorridor = FindCorridorContainingPosition(navAgent.transform.position, 3f);
    if (spawnCorridor != null)
    {
        // Controlla se il corridoio è già stato completamente esplorato in questo piano
        var poleANode = graph.GetCorridorPoleNode($"{spawnCorridor.corridorId}_A");
        bool alreadyDone = poleANode != null &&
            poleANode.edges.Exists(e => e?.id == $"central_{spawnCorridor.corridorId}"
                                    && e.state == EdgeState.Explored);

        if (alreadyDone)
        {
            Debug.Log($"[ExplMgr] Corridoio '{spawnCorridor.corridorId}' già esplorato — skip.");
            StartRotation360(isRoom: true);
        }
        else
        {
            Debug.Log($"[ExplMgr] Spawn in corridoio '{spawnCorridor.corridorId}' — avvio modalità corridoio.");
            pendingCorridorId      = spawnCorridor.corridorId;
            pendingPoleId          = "1";
            pendingPolePos         = spawnCorridor.Pole1Position;
            movingToPoleCorridorId = spawnCorridor.corridorId;
            navAgent.isStopped     = false;
            navAgent.SetDestination(spawnCorridor.Pole1Position);
            movingToPoleTimer      = 0f;
            state                  = State.MovingToPole;
        }
    }
    else
    {
        StartRotation360(isRoom: true);
    }
}
    // ====================================================
    // FINE ESPLORAZIONE
    // ====================================================

    private void OnAllFloorsCompleted()
{
    explorationVisionCone?.SetExplorationMode(false);
    state = State.Completed;

    // ── 1. Calcola distanze Stanze ────────────────────────────
    var ddm = GetComponent<RoomDistanceMap>();
    if (ddm != null)
        ddm.Compute(floorGraphs);
    else
        Debug.LogWarning("[ExplMgr] RoomDistanceMap non trovato sul GameObject!");

    List<RoomDistancePair> roomPairs = ddm?.pairs ?? new List<RoomDistancePair>();

    // ── 2. Costruisci snapshot ───────────────────────────────
    BuildingSnapshot snapshot = GraphSnapshotBuilder.Build(
        floorGraphs, ConnectorLinks, roomPairs, _roomArtifacts, buildingId);

    snapshot.discoveredObjects.AddRange(_artifactData.Values);
    // ── 3. Salva su disco ────────────────────────────────────
    bool saved = GraphPersistence.Save(snapshot);
    if (!saved)
        Debug.LogWarning("[ExplMgr] Salvataggio snapshot fallito: " +
                        "la trasmissione a JaCaMo procede comunque.");

    
    // ── 4. Salva JSON artefatti separato ─────────────────────
    ArtifactsPersistence.Save(buildingId, _roomArtifacts, _artifactData);


    // ── 6. Trasmetti a JaCaMo ────────────────────────────────
    var bt = GetComponent<BeliefTransmitter>();
    bt?.TransmitSnapshot(snapshot);

     // ── 7. Aggiorna current_room col nodo reale di fine esplorazione ──
    var loc = GetComponent<LocalizationTest>();  
    loc?.ForceCurrentRoomAndSend(NormalizeRoomId(currentNodeId));
    Debug.Log($"[ExplMgr] Edificio '{buildingId}' esplorato. " +
            $"Piani: {floorGraphs.Count}  Link: {ConnectorLinks.Count}  " +
            $"Distanze: {roomPairs.Count}  Snapshot salvato: {saved}");
}
   
    // ====================================================
    // HELPER
    // ====================================================
    private CorridorData FindCorridorData(string id)
    {
        foreach (var c in FindObjectsByType<CorridorData>(FindObjectsSortMode.None))
            if (c.corridorId == id) return c;
        return null;
    }
        private CorridorData FindCorridorContainingPosition(Vector3 pos, float radius)
    {
        foreach (var c in FindObjectsByType<CorridorData>(FindObjectsSortMode.None))
        {
            // La posizione è tra i due poli (proiezione sull'asse del corridoio)
            Vector3 axis = c.Pole2Position - c.Pole1Position;
            float len    = axis.magnitude;
            if (len < 0.01f) continue;
            Vector3 dir  = axis / len;
            float proj   = Vector3.Dot(pos - c.Pole1Position, dir);
            // Distanza laterale dall'asse
            Vector3 onAxis  = c.Pole1Position + dir * Mathf.Clamp(proj, 0f, len);
            float lateralDist = Vector3.Distance(pos, onAxis);
            if (lateralDist <= radius && proj >= -radius && proj <= len + radius)
                return c;
        }
        return null;
    }
    private string NormalizeRoomId(string nodeId)
{
    if (string.IsNullOrEmpty(nodeId)) return nodeId;
    if (nodeId.EndsWith("_A") || nodeId.EndsWith("_B"))
        return nodeId.Substring(0, nodeId.Length - 2);   // corridoio2_B → corridoio2
    return nodeId;
}

    private DoorVarcoScript FindDoorScript(string name)
    {
        string cleanName = name;
        if (name.StartsWith("fw_"))   cleanName = name.Substring(3);
        if (name.StartsWith("bw_"))   cleanName = name.Substring(3);

        var go = GameObject.Find(cleanName);
        if (go == null) return null;
        return go.GetComponentInChildren<DoorVarcoScript>();
    }

    private bool IsConnectorDoor(string doorName)
    {
        if (buildingGraph == null) return false;
        foreach (var connector in buildingGraph.connectors)
        {
            if (connector.triggerStartObject?.name == doorName) return true;
            if (connector.triggerEndObject?.name   == doorName) return true;
        }
        return false;
    }
    private VerticalConnector FindConnectorForDoor(string doorName)
{
    if (buildingGraph == null) return null;
    foreach (var c in buildingGraph.connectors)
    {
        if (c.triggerStartObject?.name == doorName) return c;
        if (c.triggerEndObject?.name == doorName) return c;
    }
    return null;
}

private void ResetCorridorPoles(string corridorId)
{
    foreach (var pole in FindObjectsByType<CorridorPoleScript>(FindObjectsSortMode.None))
    {
        if (pole.corridorId == corridorId)
            pole.ResetTrigger();
    }
}

//PORTE PER LE SCALE
private IEnumerator TraverseStairsThen(int targetFloor, bool ascending)
{
    var activeConnector = pendingConnector;

    // Apri porta di partenza
    bool isNormalDir = (activeConnector == null || activeConnector.floorFrom != targetFloor);
    var departureDoorObj = isNormalDir
        ? activeConnector?.triggerStartObject
        : activeConnector?.triggerEndObject;

    if (departureDoorObj != null)
    {
        var departureDoor = departureDoorObj.GetComponentInChildren<DoorVarcoScript>();
        if (departureDoor != null && !departureDoor.IsTraversable)
        {
            departureDoor.TryOpen();
            yield return new WaitForSeconds(0.6f);
        }
    }

    // Aspetta che il NavMesh porti l'agente fisicamente all'altro piano
    // (il NavMesh cammina sulle scale autonomamente)
    Vector3 destination = isNormalDir
        ? activeConnector.triggerEndPosition
        : activeConnector.triggerStartPosition;

    navAgent.isStopped = false;
    navAgent.SetDestination(destination);

    // Aspetta l'arrivo
    yield return new WaitForSeconds(0.5f); // piccolo delay per far partire il path
    while (navAgent.pathPending ||
           navAgent.remainingDistance > arrivalThreshold * 2f)
    {
        yield return null;
    }

    // Apri porta di arrivo
    var arrivalDoorObj = isNormalDir
        ? activeConnector?.triggerEndObject
        : activeConnector?.triggerStartObject;

    if (arrivalDoorObj != null)
    {
        var arrivalDoor = arrivalDoorObj.GetComponentInChildren<DoorVarcoScript>();
        if (arrivalDoor != null && !arrivalDoor.IsTraversable)
        {
            arrivalDoor.TryOpen();
            yield return new WaitForSeconds(0.6f);
        }
    }

    ExecuteFloorChange(targetFloor);
}

private void ScanCorridorDoorsFromScript(string corridorId)
{
    foreach (var door in FindObjectsByType<DoorVarcoScript>(FindObjectsSortMode.None))
    {
        string frontName = door.roomFront?.name ?? "";
        string backName  = door.roomBack?.name  ?? "";

        if (frontName != corridorId && backName != corridorId) continue;

       bool isCorrLink = door.isCorridorLink;
        if (registeredTransitDoors.Contains(door.gameObject.name) && !isCorrLink) continue;
        if (allCorridorDoors.Contains(door.gameObject.name) && !isCorrLink) continue;

        float dA = Vector3.Distance(door.transform.position, poleAPos);
        float dB = Vector3.Distance(door.transform.position, poleBPos);
        string otherSide = (frontName == corridorId) ? backName : frontName;
        string side = ComputeSideRelativeToCorridorAxis(door.transform.position, poleAPos, poleBPos);

        
       if (isCorrLink)
{
    string poleBId = $"{corridorId}_B";
    float dist = Vector3.Distance(
        graph.GetNode(poleBId)?.position ?? door.transform.position,
        door.transform.position);
    var corrLinkEdge = graph.AddRoomDoorEdge(poleBId, door.gameObject.name,
        false, door.transform.position, dist,
        toNodeId: otherSide);

    // Marca subito esplorato se il corridoio destinazione è già visitato
    var targetCorrNode = graph.GetNode(otherSide) ?? graph.GetNode($"{otherSide}_A");
    bool targetAlreadyVisited = targetCorrNode != null && targetCorrNode.physicallyVisited;
    if (corrLinkEdge != null && targetAlreadyVisited)
        graph.MarkEdgeExplored(poleBId, corrLinkEdge.id, otherSide);
}
        else
        {
            graph.AddDoorEdges(corridorId, door.gameObject.name,
                door.elementType == DoorVarcoScript.ElementType.Door,
                door.transform.position, side, dA, dB, 0,
                isCorridorLink: false);
        }

        registeredTransitDoors.Add(door.gameObject.name);
        allCorridorDoors.Add(door.gameObject.name);
    }
}
private void UpdateIdle()
{
    if (string.IsNullOrEmpty(idleTargetCorridorId)) return;
    if (navAgent.pathPending) return;

    idleTimer += Time.deltaTime;

    bool arrived  = navAgent.hasPath && navAgent.remainingDistance <= arrivalThreshold;
    bool timedOut = idleTimer >= 2.0f;

    if (!arrived && !timedOut) return;

    idleTimer = 0f;
    var corridorData = FindCorridorData(idleTargetCorridorId);
    if (corridorData == null) { idleTargetCorridorId = null; return; }

    Vector3 myPos     = transform.position;
    float   distPole1 = Vector3.Distance(myPos, corridorData.Pole1Position);
    float   distPole2 = Vector3.Distance(myPos, corridorData.Pole2Position);

    bool useP1 = distPole1 <= distPole2;
    pendingCorridorId      = idleTargetCorridorId;
    pendingPoleId          = useP1 ? "1" : "2";
    pendingPolePos         = useP1 ? corridorData.Pole1Position : corridorData.Pole2Position;
    movingToPoleCorridorId = idleTargetCorridorId;
    movingToPoleTimer      = 0f;

    navAgent.isStopped = false;
    navAgent.SetDestination(pendingPolePos);
    state = State.MovingToPole;
}
private void ScanRoomDoorsFromScript(string roomId)
{
    foreach (var door in FindObjectsByType<DoorVarcoScript>(FindObjectsSortMode.None))
    {
        if (door.roomFront == null || door.roomBack == null) continue;
        if (IsConnectorDoor(door.gameObject.name)) continue;

        string frontName = door.roomFront.name;
        string backName  = door.roomBack.name;
        if (frontName != roomId && backName != roomId) continue;

        // Con il nuovo Dictionary: controlla se già registrata da QUESTA stanza
        if (registeredRoomDoors.TryGetValue(door.gameObject.name, out string from)
            && from == roomId) continue;
        string roomBeyond = (frontName == roomId) ? backName : frontName;

        // ← NON aggiungere se la destinazione è già stata fisicamente visitata
        var beyondNode = graph.GetNode(roomBeyond);
        if (beyondNode != null && beyondNode.physicallyVisited) continue;

        bool leadsToCorridor = FindCorridorData(roomBeyond) != null;
         if (leadsToCorridor)
        {
            // Se la stanza corrente ha poli propri, aggiungi arco dal polo B
            bool currentHasPoles = FindCorridorData(roomId) != null;
            if (currentHasPoles)
            {
                string poleBId = $"{roomId}_B";
                float dist = door.NavMeshDistanceTo(transform.position);
                graph.AddRoomDoorEdge(poleBId, door.gameObject.name,
                    door.elementType == DoorVarcoScript.ElementType.Door,
                    door.transform.position, dist,
                    toNodeId: roomBeyond);
            }
            continue;
        }

        registeredRoomDoors[door.gameObject.name] = roomId;
        door.discovered = true;

        float navDist = door.NavMeshDistanceTo(transform.position);
        
        graph.AddRoomDoorEdge(roomId, door.gameObject.name,
            door.elementType == DoorVarcoScript.ElementType.Door,
            door.transform.position, navDist,
            toNodeId: leadsToCorridor ? roomBeyond : null);
    }
}

private string FindDoorBetweenNodes(string nodeA, string nodeB)
{
    foreach (var conn in buildingGraph?.connectors ?? new List<VerticalConnector>())
    {
        bool aIsStart = conn.triggerStartObject != null &&
                        (conn.id == nodeA || conn.triggerStartObject.name == nodeA);
        bool aIsEnd   = conn.triggerEndObject != null &&
                        (conn.id == nodeA || conn.triggerEndObject.name == nodeA);

        // ← FIX: verifica anche che nodeB corrisponda al lato opposto
        bool bMatchesEnd   = conn.triggerEndObject   != null &&
                             (conn.id == nodeB || conn.triggerEndObject.name   == nodeB);
        bool bMatchesStart = conn.triggerStartObject != null &&
                             (conn.id == nodeB || conn.triggerStartObject.name == nodeB);

        if (aIsStart && bMatchesEnd && conn.triggerEndObject != null)
            return conn.triggerEndObject.name;
        if (aIsEnd && bMatchesStart && conn.triggerStartObject != null)
            return conn.triggerStartObject.name;
    }
    // fallback DoorVarcoScript — invariato
    foreach (var d in FindObjectsByType<DoorVarcoScript>(FindObjectsSortMode.None))
    {
        bool aFront = d.roomFront != null && d.roomFront.name == nodeA;
        bool aBack  = d.roomBack  != null && d.roomBack.name  == nodeA;
        bool bFront = d.roomFront != null && d.roomFront.name == nodeB;
        bool bBack  = d.roomBack  != null && d.roomBack.name  == nodeB;
        if ((aFront || aBack) && (bFront || bBack))
            return d.gameObject.name;
    }
    return null;
}

private float NavMeshDist(Vector3 a, Vector3 b)
{
    var path = new NavMeshPath();
    if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path)) return -1f;
    if (path.status == NavMeshPathStatus.PathInvalid) return -1f;
    float d = 0f;
    for (int i = 1; i < path.corners.Length; i++)
        d += Vector3.Distance(path.corners[i - 1], path.corners[i]);
    return d;
}

public void RegisterDiscoveredObject(string artifactId, string roomId,
    string artifactType, int wsPort, float x, float y, float z)
{
    // Aggiorna dato completo
    _artifactData[artifactId] = new RoomObjectSnapshot
    {
        artifactId   = artifactId,
        artifactType = artifactType,
        roomId       = roomId,
        wsPort       = wsPort,
        x = x, y = y, z = z,
    };

    // Rimuovi da stanza precedente se cambiata
    foreach (var kv in _roomArtifacts)
        if (kv.Key != roomId)
            kv.Value.Remove(artifactId);

    // Aggiungi alla stanza corrente
    if (!_roomArtifacts.ContainsKey(roomId))
        _roomArtifacts[roomId] = new HashSet<string>();
    _roomArtifacts[roomId].Add(artifactId);
}
}