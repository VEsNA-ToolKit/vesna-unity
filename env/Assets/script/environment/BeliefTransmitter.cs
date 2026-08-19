using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// ============================================================
// BeliefTransmitter.cs
// ============================================================
// Trasmette lo snapshot dell'edificio a JaCaMo come credenze.
//
// FORMATO CREDENZE (compatibile con Jason .asl):
//   node(ID, Type, Floor, X, Y, Z)                  ← nodi stanza/corridoio
//   connector_link(ID, FloorA, FloorB, ...)          ← nodi scala/connettore (una sola volta)
//   edge(ID, From, To, Floor, EdgeType, DoorState, ...)
//   door_dist(DoorA, DoorB, Floor, Distance)
//   exploration_complete(...)
// ============================================================

public class BeliefTransmitter : AbstractMasElement
{
    [Tooltip("Trascina qui l'avatar dell'ESPLORATORE (ExplorerAvatar). Il grafo " +
             "viaggia sul SUO canale, quello a cui è collegato il cervello explorer.")]
    public AgentAvatar avatar;  // canale verso il cervello explorer

    [Header("Trasmissione")]
    public int beliefsPerFrame = 5;
    


    // --------------------------------------------------------
    // ENTRY POINT PRINCIPALE
    // --------------------------------------------------------
    public void TransmitSnapshot(BuildingSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogError("[BeliefTransmitter] TransmitSnapshot: snapshot null.");
            return;
        }
        StartCoroutine(WaitUntilReadyThenTransmit(snapshot));//non trasmette subito, aspetta che ci sia qualcuno in ascolto(corutine)
    }

    // --------------------------------------------------------
    // WRAPPER DI COMPATIBILITÀ
    // --------------------------------------------------------
    public void TransmitGraph(//wrapper di compatibilità
    Dictionary<int, TopologicalGraph> floorGraphs,
    List<FloorConnectorLink>          connectorLinks,
    List<RoomDistancePair>            roomDistances,
    Dictionary<string, HashSet<string>> roomArtifacts = null, 
    string                            buildingId = "building")
{
    var snapshot = GraphSnapshotBuilder.Build(
        floorGraphs, connectorLinks, roomDistances, roomArtifacts, buildingId); 
    TransmitSnapshot(snapshot);
}

void Awake()
{
    if (!Application.IsPlaying(gameObject)) return;
    objInUse = gameObject;

    // Niente server proprio: il grafo viaggia sul canale dell'avatar esploratore.
    if (avatar == null) avatar = GetComponent<AgentAvatar>(); // fallback se sullo stesso GameObject
    if (avatar == null)
        Debug.LogError("[BeliefTransmitter] Campo 'avatar' non assegnato: trascina l'ExplorerAvatar in Inspector.");
}

private const float _readyPollInterval = 0.5f;
private const float _readyMaxWait      = 30f;

private IEnumerator WaitUntilReadyThenTransmit(BuildingSnapshot snapshot)
{
    float elapsed = 0f;

    while (!IsServerReady())
    {
        if (elapsed >= _readyMaxWait)
        {
            Debug.LogError("[BeliefTransmitter] Timeout: server non pronto. Trasmissione annullata.");
            yield break;
        }
        Debug.Log($"[BeliefTransmitter] Attesa server... ({elapsed:F1}s)");
        yield return new WaitForSeconds(_readyPollInterval);
        elapsed += _readyPollInterval;
    }

    while (!IsClientConnected())
    {
        if (elapsed >= _readyMaxWait)
        {
            Debug.LogError("[BeliefTransmitter] Timeout: nessun client connesso. Trasmissione annullata.");
            yield break;
        }
        Debug.Log($"[BeliefTransmitter] Attesa client... ({elapsed:F1}s)");
        yield return new WaitForSeconds(_readyPollInterval);
        elapsed += _readyPollInterval;
    }

    Debug.Log("[BeliefTransmitter] Server pronto e client connesso. Avvio trasmissione.");
    yield return StartCoroutine(TransmitSnapshotCoroutine(snapshot));
}

private bool IsServerReady()      => avatar != null && avatar.WsChannel != null && avatar.WsChannel.IsServerRunning;
private bool IsClientConnected()  => avatar != null && avatar.WsChannel != null && avatar.WsChannel.HasConnectedClients;

    // --------------------------------------------------------
    // COROUTINE PRINCIPALE
    // --------------------------------------------------------
    private IEnumerator TransmitSnapshotCoroutine(BuildingSnapshot snapshot)
    {
        Debug.Log($"[BeliefTransmitter] Inizio trasmissione snapshot '{snapshot.buildingId}' " +
                  $"({snapshot.floors.Count} piani) a JaCaMo...");

        int count = 0;

        // ── Costruisci set dei connectorId che sono scale/connettori ────
        var connectorNodeIds = new HashSet<string>();
        foreach (var link in snapshot.connectorLinks)
            connectorNodeIds.Add(link.connectorId);

        // ── Tiene traccia dei connettori già trasmessi ───────
        var sentConnectors = new HashSet<string>();

        // ── 1. Nodi e archi per piano ────────────────────────
        foreach (var floor in snapshot.floors)
        {
            // Nodi
            foreach (var node in floor.nodes)
            {
                // Nodo scala/connettore: trasmetti come connector_link una sola volta
                if (connectorNodeIds.Contains(node.nodeId))
                {
                    if (sentConnectors.Contains(node.nodeId)) continue;
                    sentConnectors.Add(node.nodeId);

                    var link = snapshot.connectorLinks.Find(l => l.connectorId == node.nodeId);

                    SendBelief(new BeliefMessage
                    {
                        beliefType = "connector_link",
                        payload    = new Dictionary<string, object>
                        {
                            { "id",            node.nodeId        },
                            { "floorA",        link.floorA        },
                            { "floorB",        link.floorB        },
                            { "bidirectional", link.bidirectional  },
                            { "posAx",         link.posAx         },
                            { "posAy",         link.posAy         },
                            { "posAz",         link.posAz         },
                            { "posBx",         link.posBx         },
                            { "posBy",         link.posBy         },
                            { "posBz",         link.posBz         },
                        }
                    });

                    if (++count % beliefsPerFrame == 0) yield return null;
                    continue;
                }

                // Nodo normale (stanza o polo corridoio)
                SendBelief(new BeliefMessage
                {
                    beliefType = "node",
                    payload    = new Dictionary<string, object>
                    {
                        { "id",            node.nodeId           },
                        { "type",          node.nodeType         },
                        { "floor",         floor.floorIndex      },
                        { "x",             node.x                },
                        { "y",             node.y                },
                        { "z",             node.z                },
                        { "corridorId",    node.corridorId ?? "" },
                        { "poleLabel",     node.poleLabel  ?? "" },
                        { "wsPort",        node.wsPort           },
                    }
                });

                if (++count % beliefsPerFrame == 0) yield return null;
            }
            // ── Credenze corridoio ───────────────────────────────
            var corridorsSent = new HashSet<string>();
          foreach (var floorSnap in snapshot.floors)
            {
                foreach (var node in floorSnap.nodes)
                {
                       if (node.nodeType != "CorridorPoleA") continue;

                            var poleB = floor.nodes.Find(n =>
                                n.corridorId == node.corridorId && n.nodeType == "CorridorPoleB");
                            if (poleB == null) continue;
                    SendBelief(new BeliefMessage
                    {
                        beliefType = "corridor",
                        payload = new Dictionary<string, object>
                        {
                            { "corridorId", node.corridorId      },
                            { "poleA",      node.nodeId           },
                            { "poleB",      poleB.nodeId          },
                            { "floor",      floorSnap.floorIndex  },
                            { "wsPort",     node.wsPort           },
                        }
                    });
                    if (++count % beliefsPerFrame == 0) yield return null;
                }
            }

            // Archi
            foreach (var edge in floor.edges)
            {
                SendBelief(new BeliefMessage
                {
                    beliefType = "edge",
                    payload    = new Dictionary<string, object>
                    {
                        { "id",              edge.edgeId           },
                        { "from",            edge.fromNodeId       },
                        { "to",              edge.toNodeId         },
                        { "floor",           floor.floorIndex      },
                        { "edgeType",        edge.edgeType         },
                        { "edgeState",       edge.edgeState        },
                        { "doorState",       edge.doorState        },
                        { "isCorridorLink",  edge.isCorridorLink   },
                        { "isPhysical",      edge.isPhysical       },
                        { "side",            edge.side ?? ""       },
                        { "distFromA",       edge.distFromA        },
                        { "distFromB",       edge.distFromB        },
                        { "orderFW",         edge.orderFW          },
                        { "orderBW",         edge.orderBW          },
                        //{ "explorationOrder",edge.explorationOrder },
                        { "x",               edge.x                },
                        { "y",               edge.y                },
                        { "z",               edge.z                },
                        { "wsPort",          edge.wsPort           },
                    }
                });

                if (++count % beliefsPerFrame == 0) yield return null;
            }
        }

        // ── 2. Distanze porte ────────────────────────────────
        foreach (var dist in snapshot.roomDistances)
        {
            SendBelief(new BeliefMessage
            {
                beliefType = "door_dist",
                payload    = new Dictionary<string, object>
                {
                    { "roomA",    dist.roomA    },
                    { "roomB",    dist.roomB    },
                    { "floor",    dist.floor    },
                    { "distance", dist.distance },
                }
            });

            if (++count % beliefsPerFrame == 0) yield return null;
        }
                // ── 2.5 Oggetti scoperti nelle stanze ────────────────
            if (snapshot.discoveredObjects != null)
            {
                foreach (var obj in snapshot.discoveredObjects)
                {
                    SendRoomObjectDiscovered(
                        obj.artifactId, obj.roomId, obj.artifactType, obj.wsPort,
                        obj.x, obj.y, obj.z);
                    if (++count % beliefsPerFrame == 0) yield return null;
                }
            }

        // ── 3. Segnale di completamento ──────────────────────
        int totalNodes = 0, totalEdges = 0;
        foreach (var f in snapshot.floors)
        {
            totalNodes += f.nodes.Count;
            totalEdges += f.edges.Count;
        }


        SendBelief(new BeliefMessage
        {
            beliefType = "exploration_complete",
            payload    = new Dictionary<string, object>
            {
                { "buildingId",  snapshot.buildingId          },
                { "capturedAt",  snapshot.capturedAt          },
                { "totalFloors", snapshot.floors.Count        },
                { "totalNodes",  totalNodes                   },
                { "totalEdges",  totalEdges                   },
                { "totalDists",  snapshot.roomDistances.Count },
            }
        });

        Debug.Log($"[BeliefTransmitter] Trasmissione completata: {count} credenze inviate.");
    }

    // --------------------------------------------------------
    // INVIO SINGOLO MESSAGGIO
    // --------------------------------------------------------
private void SendBelief(BeliefMessage msg)
{
    string json = JsonConvert.SerializeObject(msg);
    string fullMsg = UnityJacamoIntegrationUtil.CreateAndConvertJacamoMessageIntoJsonString(
        msg.beliefType, null, null, null, json
    );

    // Il canale esclusivo dell'avatar è già il destinatario.
    avatar?.SendMessageToJaCaMoBrain(fullMsg);
}

    // --------------------------------------------------------
// OGGETTI SCOPERTI A RUNTIME
// Chiamato da RoomObjectBridge quando il cono visivo vede
// un oggetto per la prima volta. Invia new_object all'agente
// Jason che crea dinamicamente RoomObjectArtifact e fa focus.
// --------------------------------------------------------
public void SendRoomObjectDiscovered(
    string artifactId, string roomId, string artifactType, int wsPort,
    float x, float y, float z)
{
    SendBelief(new BeliefMessage
    {
        beliefType = "new_object",
        payload    = new Dictionary<string, object>
        {
            { "artifactId",   artifactId   },
            { "roomId",       roomId       },
            { "artifactType", artifactType },
            { "wsPort",       wsPort       },
            { "x",            x            },
            { "y",            y            },
            { "z",            z            },
        }
    });
}

    // --------------------------------------------------------
    // DTO interno
    // --------------------------------------------------------
    [System.Serializable]
    private class BeliefMessage
    {
        public string                     beliefType;
        public Dictionary<string, object> payload;
    }
}