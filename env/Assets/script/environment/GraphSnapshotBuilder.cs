using System.Collections.Generic;
using UnityEngine;

// ============================================================
// GraphSnapshotBuilder.cs
// ============================================================
// Converte il grafo vivo (TopologicalGraph) in un BuildingSnapshot
// serializzabile e persistibile su disco.
//
// RESPONSABILITÀ UNICA:
//   Tradurre i tipi runtime (GraphNode, GraphEdge, Vector3, enum)
//   nei corrispondenti tipi DTO di BuildingSnapshot.
//   Non sa nulla di WebSocket, JaCaMo, o filesystem.
//
// COME ESTENDERE:
//   • Nuovo campo in GraphNode/GraphEdge → aggiungi il mapping
//     in NodeFrom() / EdgeFrom() e il campo in NodeSnapshot/EdgeSnapshot.
//   • Nuovo tipo di nodo → aggiungi un branch in NodeFrom() se
//     ha campi specifici (es. corridorId per i poli), altrimenti
//     nessuna modifica necessaria.
//   • Nuovo tipo di link cross-floor → aggiungi un ConnectorFrom()
//     analogo e chiamalo in Build().
// ============================================================

public static class GraphSnapshotBuilder
{
    // --------------------------------------------------------
    // ENTRY POINT
    // --------------------------------------------------------

    /// <summary>
    /// Costruisce uno snapshot completo dell'edificio a partire dai
    /// dati runtime di ExplorationManager.
    /// </summary>
    /// <param name="floorGraphs">Dizionario piano → grafo topologico.</param>
    /// <param name="connectorLinks">Link cross-floor (scale, ascensori).</param>
    /// <param name="roomDistances">Coppie di distanza NavMesh tra porte.</param>
    /// <param name="buildingId">Nome/ID logico dell'edificio (es. "Edificio_A").</param>
    public static BuildingSnapshot Build(
        Dictionary<int, TopologicalGraph> floorGraphs,
        List<FloorConnectorLink>          connectorLinks,
        List<RoomDistancePair>            roomDistances,
        Dictionary<string, HashSet<string>> roomArtifacts,
        string                            buildingId = "building")
    {
        var snapshot = new BuildingSnapshot
        {
            buildingId   = buildingId,
            capturedAt   = System.DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
        };

        // ── Piani ────────────────────────────────────────────
        var sortedFloors = new List<int>(floorGraphs.Keys);
        sortedFloors.Sort();

        foreach (int floorIndex in sortedFloors)
        {
            var floor    = new FloorSnapshot { floorIndex = floorIndex };
            var graph    = floorGraphs[floorIndex];

            foreach (var node in graph.AllNodes())
            {
                if (node == null) continue;
                var nodeSnap = NodeFrom(node); // ← salva in variabile locale

               if (roomArtifacts != null)
                {
                    // Per i nodi normali cerca per node.id; per i poli corridoio
                    // cerca anche per corridorId (es. "Corridoio2") perché il
                    // roomId degli artifact punta al corridoio, non al polo specifico.
                    string lookupKey = node.id;
                    if (node is CorridorPoleNode pole && !roomArtifacts.ContainsKey(node.id))
                        lookupKey = pole.corridorId;

                    if (roomArtifacts.TryGetValue(lookupKey, out var artifactNames))
                        nodeSnap.artifacts.AddRange(artifactNames);
                }

                floor.nodes.Add(nodeSnap); // ← aggiungi dopo aver popolato


                foreach (var edge in node.edges)
                {
                    if (edge == null) continue;
                    // Gli archi sono salvati con il floorIndex per ricollegarli
                    // al piano corretto al momento del caricamento.
                    floor.edges.Add(EdgeFrom(edge));
                }
            }

            snapshot.floors.Add(floor);
        }

        // ── Connettori cross-floor ───────────────────────────
        foreach (var link in connectorLinks)
            snapshot.connectorLinks.Add(ConnectorLinkFrom(link));

        // ── Distanze porte ───────────────────────────────────
        foreach (var pair in roomDistances)
            snapshot.roomDistances.Add(RoomDistFrom(pair));

        
        Debug.Log($"[SnapshotBuilder] Snapshot creato: " +
                  $"{snapshot.floors.Count} piani, " +
                  $"{snapshot.connectorLinks.Count} link, " +
                  $"{snapshot.roomDistances.Count} distanze porte.");

       
        return snapshot;

    }

    // --------------------------------------------------------
    // CONVERSORI PRIVATI
    // Ogni metodo converte un solo tipo runtime → DTO.
    // Facili da trovare, facili da modificare indipendentemente.
    // --------------------------------------------------------

    private static NodeSnapshot NodeFrom(GraphNode node)
    {
        var s = new NodeSnapshot
        {
            nodeId           = node.id,
            nodeType         = node.type.ToString(),
            x                = node.position.x,
            y                = node.position.y,
            z                = node.position.z,
            
        };

        // Campi specifici dei nodi polo corridoio
        if (node is CorridorPoleNode pole)
        {
            s.corridorId = pole.corridorId;
            s.poleLabel  = pole.poleLabel;
           if (int.TryParse(PortAssigner.GetPort(pole.corridorId), out int p))
                s.wsPort = p;
        }

        return s;
    }

   private static EdgeSnapshot EdgeFrom(GraphEdge edge)
{
    var s = new EdgeSnapshot
    {
        edgeId         = edge.id,
        fromNodeId     = edge.fromNodeId,
        toNodeId       = edge.toNodeId ?? "unknown",
        edgeType       = edge.edgeType.ToString(),
        edgeState      = edge.state.ToString(),
        /*doorState      = edge.doorState.ToString(),
        isCorridorLink = edge.isCorridorLink,*/
        x              = edge.position.x,
        y              = edge.position.y,
        z              = edge.position.z,
    };

    // ── Logica condizionale per i campi da escludere negli archi Central ──
    if (edge.edgeType != EdgeType.Central)
    {
        s.doorState      = edge.doorState.ToString();
        s.isCorridorLink = edge.isCorridorLink;
    }

    // isPhysical riflette l'elementType reale (Door/Varco) impostato in
    // Inspector su DoorVarcoScript, non il nome del GameObject.
    s.isPhysical = edge.isPhysical;

    // Campi solo per archi corridoio
    if (edge.edgeType == EdgeType.DoorFW ||
        edge.edgeType == EdgeType.DoorBW)
    {
        s.side             = edge.side;
        s.distFromA        = edge.distFromA;
        s.distFromB        = edge.distFromB;
        s.orderFW          = edge.orderFW;
        s.orderBW          = edge.orderBW;
        s.explorationOrder = edge.explorationOrder;
    }

    if (edge.edgeType == EdgeType.Central)
    {
        s.distance = edge.distFromA; // distFromA contiene la lunghezza totale del corridoio
    }
    string goName = ResolveGameObjectName(edge.id);
    if (goName != null)
    {
        var go = GameObject.Find(goName);
        if (go != null)
        {
            var bridge = go.GetComponent<DoorVarcoArtifactBridge>();
            if (bridge != null && int.TryParse(bridge.port, out int p))
                s.wsPort = p;
        }
    }

        return s;
    }
   private static ConnectorLinkSnapshot ConnectorLinkFrom(FloorConnectorLink link)
    {
        return new ConnectorLinkSnapshot
        {
            connectorId   = link.connectorId,
            floorA        = link.floorFrom,
            floorB        = link.floorTo,
            bidirectional = link.bidirectional,
            posAx         = link.posFrom.x,
            posAy         = link.posFrom.y,
            posAz         = link.posFrom.z,
            posBx         = link.posTo.x,
            posBy         = link.posTo.y,
            posBz         = link.posTo.z,
        };
    }

    private static RoomDistSnapshot RoomDistFrom(RoomDistancePair pair)
    {
        return new RoomDistSnapshot
        {
            roomA    = pair.roomA,
            roomB    = pair.roomB,
            floor    = pair.floor,
            distance = pair.distance,
            doorName = pair.doorName,
        };
    }
    private static string ResolveGameObjectName(string edgeId)
{
    if (edgeId.StartsWith("fw_"))      return edgeId.Substring(3);
    if (edgeId.StartsWith("bw_"))      return edgeId.Substring(3);
    if (edgeId.StartsWith("seg_"))     return edgeId.Substring(4);
    if (edgeId.StartsWith("central_")) return null;
    return edgeId;
}
}
