using System.Collections.Generic;using System.Collections.Generic;using System.Collections.Generic;
using UnityEngine;

// ============================================================
// TopologicalGraph.cs  — v3
// ============================================================
// LIVELLO 1 (Piano): ogni corridoio ha DUE nodi polo A e B.
//   corridoioX_A ──[central_corridoioX]──► corridoioX_B
//   corridoioX_A ──[fw_porta]──► porta    (distFromA, orderFW per lato)
//   corridoioX_B ──[bw_porta]──► porta    (distFromB, orderBW per lato)
//
// NOVITÀ v3:
//   - orderFW e orderBW sono PER LATO (RIGHT e LEFT scale separate)
//     es: bagno=fw2R, camera=fw1R, cucina=fw1L  → nessun buco nei numeri
//   - explorationOrder: sequenza di visita effettiva dal polo B
//     calcolata da ComputeExplorationOrder (clustering + zigzag R/L)
//   - NextCorridorEdgeToInspect usa explorationOrder come fonte unica
//
// LIVELLO 2 (Stanza): nodo stanza con archi verso porte/varchi.
//   roomNode ──[room_porta]──► porta      (navMeshDist, greedy)
// ============================================================

public enum NodeType  { Unknown, CorridorPoleA, CorridorPoleB, Room }
public enum EdgeState { Discovered, Explored }
public enum DoorState { Open, Closed, Locked }
public enum EdgeType  { Central, DoorFW, DoorBW, Door, Segment }

// ─────────────────────────────────────────────────────────────
// NODO BASE (astratto)
// ─────────────────────────────────────────────────────────────
[System.Serializable]
public abstract class GraphNode
{
    public string   id;
    public NodeType type;
    public Vector3  position;
    public bool     fullyExplored;

    public List<GraphEdge> edges = new List<GraphEdge>();

    /// <summary>
    /// True dopo che l'agente ha eseguito un 360° fisico in questa stanza.
    /// Usato per distinguere nodi aggiunti programmaticamente (es. ExecuteFloorChange)
    /// da nodi realmente visitati, evitando che LOOP EVITATO blocchi l'ingresso.
    /// </summary>
    public bool physicallyVisited = false;

    protected GraphNode(string id, NodeType type, Vector3 position)
    {
        this.id       = id;
        this.type     = type;
        this.position = position;
        fullyExplored = false;
    }

    public bool HasUndiscoveredEdges()
    {
        foreach (var e in edges)
            if (e != null && e.state == EdgeState.Discovered) return true;
        return false;
    }

    public int EdgeCount => edges.Count;
}

// ─────────────────────────────────────────────────────────────
// NODO STANZA
// ─────────────────────────────────────────────────────────────
[System.Serializable]
public class RoomNode : GraphNode
{
    public RoomNode(string id, Vector3 position)
        : base(id, NodeType.Room, position) { }
}

// ─────────────────────────────────────────────────────────────
// NODO POLO CORRIDOIO
// ─────────────────────────────────────────────────────────────
[System.Serializable]
public class CorridorPoleNode : GraphNode
{
    public string corridorId;
    public string poleLabel;  // "A" o "B"

    public CorridorPoleNode(string id, NodeType type, Vector3 position,
                             string corridorId, string poleLabel)
        : base(id, type, position)
    {
        this.corridorId = corridorId;
        this.poleLabel  = poleLabel;
    }
}

// ─────────────────────────────────────────────────────────────
// ARCO
// ─────────────────────────────────────────────────────────────
[System.Serializable]
public class GraphEdge
{
    public string    id;
    public string    fromNodeId;
    public string    toNodeId;
    public EdgeState state;
    public EdgeType  edgeType;
    public DoorState doorState;
    public bool      isPhysical;
    public string    side;
    public float     distFromA;
    public float     distFromB;
    public int       orderFW;           // rank per lato da A (1=primo su quel lato)
    public int       orderBW;           // rank per lato da B (1=primo su quel lato)
    public int       explorationOrder;  // sequenza visita effettiva dal polo B
    public float     navMeshDist;
    public Vector3   position;

    public bool isCorridorLink; // aggiunto per gestire varchi che sono collegamento di corridoio . se no si alterava la struttura del grafo

    public string doorName;
    public GraphEdge(string id, string fromNodeId, bool isPhysical,
                     EdgeType edgeType, Vector3 position)
    {
        this.id         = id;
        this.fromNodeId = fromNodeId;
        this.isPhysical = isPhysical;
        this.edgeType   = edgeType;
        this.position   = position;
        this.state      = EdgeState.Discovered;
        this.doorState  = isPhysical ? DoorState.Closed : DoorState.Open;
        this.toNodeId   = null;
    }
}

// ─────────────────────────────────────────────────────────────
// GRAFO
// ─────────────────────────────────────────────────────────────
public class TopologicalGraph
{
    private Dictionary<string, GraphNode> nodes = new Dictionary<string, GraphNode>();
    private Stack<string> explorationStack       = new Stack<string>();

    public string CurrentNodeId { get; private set; }

    // =========================================================
    // AGGIUNTA NODI
    // =========================================================

    public RoomNode AddRoomNode(string id, Vector3 position)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[Graph] AddRoomNode: id nullo/vuoto ignorato.");
            return null;
        }
        if (nodes.ContainsKey(id))
            return nodes[id] as RoomNode;

        var node = new RoomNode(id, position);
        nodes[id] = node;
        Debug.Log($"[Graph] Nodo aggiunto: {id} (Room)");
        return node;
    }

    public GraphNode AddNode(string id, NodeType type, Vector3 position)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[Graph] AddNode: id nullo/vuoto ignorato.");
            return null;
        }
        if (nodes.ContainsKey(id)) return nodes[id];
        return AddRoomNode(id, position);
    }

    // Crea coppia polo A + polo B + arco centrale (idempotente)
    public (GraphNode poleA, GraphNode poleB) AddCorridorPoles(
        string corridorId, Vector3 posA, Vector3 posB, float centralLength)
    {
        string idA = $"{corridorId}_A";
        string idB = $"{corridorId}_B";

        GraphNode nodeA, nodeB;
        if (!nodes.ContainsKey(idA))
        {
            var poleA = new CorridorPoleNode(idA, NodeType.CorridorPoleA, posA, corridorId, "A");
            nodes[idA] = poleA;
            nodeA = poleA;
            Debug.Log($"[Graph] Nodo aggiunto: {idA} (CorridorPoleA)");
        }
        else nodeA = nodes[idA];

        if (!nodes.ContainsKey(idB))
        {
            var poleB = new CorridorPoleNode(idB, NodeType.CorridorPoleB, posB, corridorId, "B");
            nodes[idB] = poleB;
            nodeB = poleB;
            Debug.Log($"[Graph] Nodo aggiunto: {idB} (CorridorPoleB)");
        }
        else nodeB = nodes[idB];

        string centralId = $"central_{corridorId}";
        if (nodeA != null && nodeA.edges.Find(e => e?.id == centralId) == null)
        {
            var central = new GraphEdge(centralId, idA, false, EdgeType.Central, posB)
            {
                toNodeId  = idB,
                distFromA = centralLength,
                state     = EdgeState.Discovered
            };
            nodeA.edges.Add(central);
            Debug.Log($"[Graph] Arco centrale: {idA}→{idB} len={centralLength:F1}");
        }

        return (nodeA, nodeB);
    }

    // =========================================================
    // AGGIUNTA ARCHI PORTA — doppio FW (da A) e BW (da B)
    // NOTA: orderFW passato dall'esterno NON viene più usato per
    //       l'ordinamento — viene ricalcolato internamente per lato
    //       da ComputeForwardOrders(). Il parametro rimane per
    //       compatibilità della firma ma viene ignorato.
    // =========================================================
    public void AddDoorEdges(string corridorId, string doorId, bool isPhysical,
                             Vector3 doorPos, string side,
                             float distFromA, float distFromB, int orderFW, bool isCorridorLink = false)
    {
        string idA = $"{corridorId}_A";
        string idB = $"{corridorId}_B";

        if (!nodes.ContainsKey(idA) || !nodes.ContainsKey(idB))
        {
            Debug.LogWarning($"[Graph] AddDoorEdges: poli {corridorId} non ancora creati!");
            return;
        }

        var nodeA = nodes[idA];
        var nodeB = nodes[idB];

        // Arco FW: polo A → porta
        string fwId = $"fw_{doorId}";
        if (nodeA.edges.Find(e => e?.id == fwId) == null)
        {
            nodeA.edges.Add(new GraphEdge(fwId, idA, isPhysical, EdgeType.DoorFW, doorPos)
            {
                toNodeId  = doorId,
                side      = side,
                distFromA = distFromA,
                distFromB = distFromB,
                orderFW   = 0,   // verrà assegnato da ComputeForwardOrders()
                 isCorridorLink = isCorridorLink,   // ← imposta
                  doorName       = doorId 
            });
        }

        // Arco BW: polo B → porta
        string bwId = $"bw_{doorId}";
       string bwSide = (side == "LEFT") ? "RIGHT" : "LEFT";
        if (nodeB.edges.Find(e => e?.id == bwId) == null)
        {
            nodeB.edges.Add(new GraphEdge(bwId, idB, isPhysical, EdgeType.DoorBW, doorPos)
            {
                toNodeId       = doorId,
                side           = bwSide,   // ← invertito rispetto al fw
                distFromA      = distFromA,
                distFromB      = distFromB,
                orderBW        = 0,
                isCorridorLink = isCorridorLink,
                 doorName       = doorId 
            });
        }

        Debug.Log($"[Graph] DoorEdges: {corridorId}↔{doorId} side={side} dA={distFromA:F1} dB={distFromB:F1}");
    }

    // Arco da nodo stanza → porta (scoperta col 360° interno)
    public GraphEdge AddRoomDoorEdge(string fromNodeId, string doorId, bool isPhysical,
                                     Vector3 doorPos, float navMeshDist, string toNodeId = null)
    {
        if (!nodes.ContainsKey(fromNodeId)) return null;
        var node  = nodes[fromNodeId];
        string eid = $"{doorId}";

        var existing = node.edges.Find(e => e?.id == eid);
        if (existing != null) return existing;

        var edge = new GraphEdge(eid, fromNodeId, isPhysical, EdgeType.Door, doorPos)
        {
            navMeshDist = navMeshDist,
            toNodeId    =  toNodeId ?? doorId,   // ← usa toNodeId se fornito
             doorName       = doorId 
        };
        node.edges.Add(edge);

        Debug.Log($"[Graph] Door: {fromNodeId}→{doorId} navDist={navMeshDist:F1}");
        return edge;
    }

    // Arco segmento tra fine corridoio N e inizio corridoio N+1
    public GraphEdge AddSegmentArc(string fromNodeId, string toNodeId,
                            float distance = 0.5f, string doorName = null,
                            Vector3 doorPos = default)
    {
        if (!nodes.ContainsKey(fromNodeId)) return null;
        var fromNode = nodes[fromNodeId];

        // Risolvi doorName prima di costruire l'id
        if (doorName == null)
        {
            var linkEdge = fromNode.edges.Find(
                e => e != null && e.isCorridorLink && e.state == EdgeState.Explored);
            doorName = linkEdge?.doorName ?? "";
        }

        // Id usa il nome del varco se disponibile
        string segId = !string.IsNullOrEmpty(doorName)
            ? $"seg_{doorName}"
            : $"seg_{fromNodeId}_to_{toNodeId}";

        if (fromNode.edges.Find(e => e?.id == segId) != null) return null;

        var toNode = nodes.ContainsKey(toNodeId) ? nodes[toNodeId] : null;

       var edge = new GraphEdge(segId, fromNodeId, false, EdgeType.Segment,
                        doorPos != default ? doorPos : toNode?.position ?? Vector3.zero)
        {
            toNodeId  = toNodeId,
            distFromA = distance,
            state     = EdgeState.Explored,
            doorName  = doorName,
            isCorridorLink = true
        };
        fromNode.edges.Add(edge);

        string doorLabel = !string.IsNullOrEmpty(doorName) ? $" [{doorName}]" : "";
        Debug.Log($"[Graph] Segmento: {fromNodeId}→{toNodeId}{doorLabel} dist={distance:F2}m");
        return edge;
    }
    // =========================================================
    // NAVIGAZIONE (stack DFS)
    // =========================================================

    public void EnterNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        if (!nodes.ContainsKey(nodeId))
        {
            Debug.LogError($"[Graph] EnterNode: nodo '{nodeId}' non esiste!");
            return;
        }
        explorationStack.Push(nodeId);
        CurrentNodeId = nodeId;
        Debug.Log($"[Graph] Enter: {nodeId} (depth={explorationStack.Count})");
    }

    public string Backtrack()
    {
        if (explorationStack.Count == 0) return null;
        explorationStack.Pop();
        if (explorationStack.Count == 0)
        {
            CurrentNodeId = null;
            Debug.Log("[Graph] Stack vuoto.");
            return null;
        }
        CurrentNodeId = explorationStack.Peek();
        Debug.Log($"[Graph] Backtrack → {CurrentNodeId}");
        return CurrentNodeId;
    }

    // =========================================================
    // MARK EXPLORED
    // =========================================================

    public void MarkEdgeExplored(string fromNodeId, string edgeId, string toNodeId = null)
    {
        if (!nodes.ContainsKey(fromNodeId)) return;
        var edge = nodes[fromNodeId].edges.Find(e => e?.id == edgeId);
        if (edge == null) return;
        edge.state = EdgeState.Explored;
        if (toNodeId != null) edge.toNodeId = toNodeId;
        Debug.Log($"[Graph] Explored: {fromNodeId}→{edgeId} (→{edge.toNodeId ?? "?"})");
    }

    public void UpdateDoorState(string edgeId, DoorState state)
    {
        foreach (var node in nodes.Values)
        {
            var edge = node.edges.Find(e => e?.id == edgeId);
            if (edge != null) { edge.doorState = state; return; }
        }
    }

    // =========================================================
    // CALCOLO ORDINI — tre metodi da chiamare IN SEQUENZA
    // Chiamata corretta (vedi ExplorationManager):
    //   1. ComputeForwardOrders(corridorId)
    //   2. ComputeBackwardOrders(corridorId)
    //   3. ComputeExplorationOrder(corridorId)
    // =========================================================

    // ── 1. orderFW per lato (scala RIGHT separata da LEFT) ──────────────────
    // Porta più vicina ad A su quel lato = fw1R o fw1L, poi fw2R, fw2L, ecc.
    // Non ci sono mai buchi nei numeri perché ogni scala è indipendente.
    public void ComputeForwardOrders(string corridorId)
    {
        string idA = $"{corridorId}_A";
        if (!nodes.ContainsKey(idA)) return;

        var fwEdges = nodes[idA].edges.FindAll(
    e => e != null && e.edgeType == EdgeType.DoorFW
                   && !e.isCorridorLink);  

        var fwRight = fwEdges.FindAll(e => e.side == "RIGHT");
        var fwLeft  = fwEdges.FindAll(e => e.side == "LEFT");

        // Ordine crescente per distFromA: la più vicina ad A è la prima
        fwRight.Sort((a, b) => a.distFromA.CompareTo(b.distFromA));
        fwLeft .Sort((a, b) => a.distFromA.CompareTo(b.distFromA));

        for (int i = 0; i < fwRight.Count; i++) fwRight[i].orderFW = i + 1;
        for (int i = 0; i < fwLeft.Count;  i++) fwLeft[i].orderFW  = i + 1;

        // Propaga orderFW sugli archi BW gemelli (stesso toNodeId)
        string idB = $"{corridorId}_B";
        if (nodes.ContainsKey(idB))
        {
            foreach (var fw in fwEdges)
            {
                var bw = nodes[idB].edges.Find(
                    e => e != null && e.edgeType == EdgeType.DoorBW
                                   && e.toNodeId == fw.toNodeId);
                if (bw != null) bw.orderFW = fw.orderFW;
            }
        }

        Debug.Log($"[Graph] FW orders {corridorId} → " +
                  $"RIGHT:[{string.Join(",", fwRight.ConvertAll(e => $"fw{e.orderFW}R={e.toNodeId}(dA={e.distFromA:F1})"))}] " +
                  $"LEFT:[{string.Join(",",  fwLeft.ConvertAll( e => $"fw{e.orderFW}L={e.toNodeId}(dA={e.distFromA:F1})"))}]");
    }

    // ── 2. orderBW per lato (scala RIGHT separata da LEFT) ──────────────────
    // Porta più vicina a B su quel lato = bw1R o bw1L, poi bw2R, bw2L, ecc.
    public void ComputeBackwardOrders(string corridorId)
    {
        string idB = $"{corridorId}_B";
        if (!nodes.ContainsKey(idB)) return;

        var bwEdges = nodes[idB].edges.FindAll(
    e => e != null && e.edgeType == EdgeType.DoorBW
                   && !e.isCorridorLink);   // ← escludi

        var bwRight = bwEdges.FindAll(e => e.side == "RIGHT");
        var bwLeft  = bwEdges.FindAll(e => e.side == "LEFT");

        // Ordine crescente per distFromB: la più vicina a B è la prima
        bwRight.Sort((a, b) => a.distFromB.CompareTo(b.distFromB));
        bwLeft .Sort((a, b) => a.distFromB.CompareTo(b.distFromB));

        for (int i = 0; i < bwRight.Count; i++) bwRight[i].orderBW = i + 1;
        for (int i = 0; i < bwLeft.Count;  i++) bwLeft[i].orderBW  = i + 1;

        // Propaga orderBW sugli archi FW gemelli (stesso toNodeId)
        string idA = $"{corridorId}_A";
        if (nodes.ContainsKey(idA))
        {
            foreach (var bw in bwEdges)
            {
                var fw = nodes[idA].edges.Find(
                    e => e != null && e.edgeType == EdgeType.DoorFW
                                   && e.toNodeId == bw.toNodeId);
                if (fw != null) fw.orderBW = bw.orderBW;
            }
        }

        Debug.Log($"[Graph] BW orders {corridorId} → " +
                  $"RIGHT:[{string.Join(",", bwRight.ConvertAll(e => $"bw{e.orderBW}R={e.toNodeId}(dB={e.distFromB:F1})"))}] " +
                  $"LEFT:[{string.Join(",",  bwLeft.ConvertAll( e => $"bw{e.orderBW}L={e.toNodeId}(dB={e.distFromB:F1})"))}]");
    }

    // ── 3. explorationOrder: sequenza visita effettiva dal polo B ────────────
    // Algoritmo:
    //   a) Ordina tutti gli archi BW per distFromB crescente (più vicini a B prima)
    //   b) Raggruppa in cluster spaziali (porte a ≤ clusterEpsilon = stessa posizione logica)
    //   c) Dentro ogni cluster: RIGHT prima, poi LEFT (zigzag per ridurre rotazioni)
    //   d) Assegna explorationOrder progressivo
    //
    // Esempio con bagno(dB=0.1,R), camera(dB=2.0,L), cucina(dB=2.1,R):
    //   Cluster1: bagno(R)  → exp=1
    //   Cluster2: cucina(R) → exp=2,  camera(L) → exp=3
    //   Visita: bagno → cucina → camera
    public void ComputeExplorationOrder(string corridorId)
    {
        string idB = $"{corridorId}_B";
        if (!nodes.ContainsKey(idB)) return;

       var bwEdges = nodes[idB].edges.FindAll(
    e => e != null && e.edgeType == EdgeType.DoorBW
                   && !e.isCorridorLink);   // ← escludi
        if (bwEdges.Count == 0) return;

        // a) Ordina per distFromB crescente
        bwEdges.Sort((a, b) => a.distFromB.CompareTo(b.distFromB));

        // b) Clustering spaziale
        const float clusterEpsilon = 0.25f;
        var clusters = new List<List<GraphEdge>>();
        var current  = new List<GraphEdge> { bwEdges[0] };

        for (int i = 1; i < bwEdges.Count; i++)
        {
            float gap = bwEdges[i].distFromB - bwEdges[i - 1].distFromB;
            if (gap <= clusterEpsilon)
                current.Add(bwEdges[i]);
            else
            {
                clusters.Add(current);
                current = new List<GraphEdge> { bwEdges[i] };
            }
        }
        clusters.Add(current);

        // c) Dentro ogni cluster: RIGHT prima (0), LEFT dopo (1)
        int order = 1;
        foreach (var cluster in clusters)
        {
            cluster.Sort((a, b) =>
            {
                int sA = (a.side == "RIGHT") ? 0 : 1;
                int sB = (b.side == "RIGHT") ? 0 : 1;
                return sA.CompareTo(sB);
            });
            foreach (var e in cluster)
                e.explorationOrder = order++;
        }

        // Log leggibile con struttura cluster
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[Graph] explorationOrder {corridorId} — {clusters.Count} cluster:");
        int ci = 1;
        foreach (var cluster in clusters)
        {
            sb.Append($"  Cluster{ci++}(dB≈{cluster[0].distFromB:F1}): ");
            sb.AppendLine(string.Join(", ", cluster.ConvertAll(
                e => $"{e.toNodeId}→exp{e.explorationOrder}({e.side[0]})")));
        }
        Debug.Log(sb.ToString());
    }

    // =========================================================
    // QUERY
    // =========================================================

    public GraphNode GetNode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        nodes.TryGetValue(id, out var n);
        return n;
    }

    public RoomNode       GetRoomNode(string id)         => GetNode(id) as RoomNode;
    public CorridorPoleNode GetCorridorPoleNode(string id) => GetNode(id) as CorridorPoleNode;

    public IEnumerable<GraphNode> AllNodes() => nodes.Values;

    // ── Prossimo arco in corridoio dal polo B ───────────────────────────────
    // Usa explorationOrder come unica fonte di verità per la sequenza di visita.
    // explorationOrder=0 significa che ComputeExplorationOrder non è ancora stato
    // chiamato → fallback su distFromB grezzo per sicurezza.
    public GraphEdge NextCorridorEdgeToInspect(string poleBNodeId)
{
    if (!nodes.ContainsKey(poleBNodeId)) return null;
    var all = nodes[poleBNodeId].edges.FindAll(
        e => e != null
          && e.state    == EdgeState.Discovered
          && e.edgeType == EdgeType.DoorBW);
    if (all.Count == 0) return null;

    // Prima le stanze reali (isCorridorLink=false), ordinate per explorationOrder
    var rooms = all.FindAll(e => !e.isCorridorLink);
    if (rooms.Count > 0)
    {
        rooms.Sort((a, b) => a.explorationOrder.CompareTo(b.explorationOrder));
        return rooms[0];
    }

    // Solo se tutte le stanze sono già esplorate → prendi il link corridoio
    var links = all.FindAll(e => e.isCorridorLink);
    links.Sort((a, b) => a.distFromB.CompareTo(b.distFromB));
    return links.Count > 0 ? links[0] : null;
}

    // ── Prossimo arco in stanza (greedy NavMesh) ────────────────────────────
    public GraphEdge NextRoomEdgeToInspect(string roomNodeId)
    {
        if (!nodes.ContainsKey(roomNodeId)) return null;
        var list = nodes[roomNodeId].edges.FindAll(
            e => e != null && e.state == EdgeState.Discovered && e.edgeType == EdgeType.Door);
        if (list.Count == 0) return null;
        list.Sort((a, b) => a.navMeshDist.CompareTo(b.navMeshDist));
        return list[0];
    }

    // ── Fallback: qualsiasi discovered ──────────────────────────────────────
    public GraphEdge NextEdgeToInspect(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !nodes.ContainsKey(nodeId)) return null;
        var node = nodes[nodeId];
        if (node.type == NodeType.CorridorPoleB) return NextCorridorEdgeToInspect(nodeId);
        if (node.type == NodeType.Room)          return NextRoomEdgeToInspect(nodeId);
        var any = node.edges.FindAll(e => e != null && e.state == EdgeState.Discovered);
        if (any.Count == 0) return null;
        any.Sort((a, b) => a.distFromB.CompareTo(b.distFromB));
        return any[0];
    }

    // ── Conteggio discovered (debug) ────────────────────────────────────────
    public int GetDiscoveredEdgesCount(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !nodes.ContainsKey(nodeId)) return 0;
        return nodes[nodeId].edges.FindAll(
            e => e != null && e.state == EdgeState.Discovered).Count;
    }

    public string FindNearestNodeWithDiscoveredEdges(Vector3 agentPos)
    {
        string nearest = null;
        float  minDist = float.MaxValue;
        foreach (var node in nodes.Values)
        {
            if (node == null || !node.HasUndiscoveredEdges()) continue;
            float d = Vector3.Distance(agentPos, node.position);
            if (d < minDist) { minDist = d; nearest = node.id; }
        }
        return nearest;
    }

    public bool IsFullyExplored()
    {
        foreach (var node in nodes.Values)
            if (node != null && node.HasUndiscoveredEdges()) return false;
        return true;
    }

    public bool IsCorridorFullyTransited(string corridorId)
    {
        string idA = $"{corridorId}_A";
        if (!nodes.ContainsKey(idA)) return false;
        var centralEdge = nodes[idA].edges.Find(
            e => e != null && e.edgeType == EdgeType.Central);
        return centralEdge != null && centralEdge.state == EdgeState.Explored;
    }

    /*public void ResetCorridorPoles(string corridorId)
    {
        nodes.Remove($"{corridorId}_A");
        nodes.Remove($"{corridorId}_B");
    }*/

   

}