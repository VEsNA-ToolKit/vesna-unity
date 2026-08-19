using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using Newtonsoft.Json.Linq;

// ============================================================
// TopologicalMovement.cs  ->  : MovementModel
// ============================================================
// MovementModel concreto per gli agenti che navigano il grafo
// topologico (esploratore E navigatore).
//
// Sostituisce il vecchio VesnaMover e accentra QUI tutta la
// logica dei nodi che prima era sparsa in AgentAvatar (dizionario
// + loader JSON) e in BeliefTransmitter (coda di movimento).
//
// Flusso:
//   JaCaMo: reach_dest(N) -> vesna.walk(N) -> Unity Walk/goto
//   -> VesnaAvatarBase.OnMessage intercetta il nodo
//   -> GoToNode(N) -> NavMesh
//   -> all'arrivo: avatar.NotifyDestinationReached(N)
//      che diventa reached(place, N) lato Jason.
//
// Va sullo STESSO GameObject di:
//   - NavMeshAgent
//   - un avatar che estende VesnaAvatarBase (ExplorerAvatar / NavigatorAvatar)
// ============================================================

public class TopologicalMovement : MovementModel
{
    [Header("Riferimenti")]
    public NavMeshAgent agent;

    [Header("Configurazione")]
    [Tooltip("Raggio entro cui un nodo INTERMEDIO si considera 'attraversato': appena " +
             "ci entri, notifichi l'arrivo e punti al prossimo senza andare al centro. " +
             "Alzalo per passare più larghi, abbassalo per passare più vicino al nodo.")]
    public float arrivalThreshold = 1.5f;

    [Tooltip("Raggio entro cui l'avvicinamento finale a un OGGETTO si considera concluso. " +
         "Più piccolo di arrivalThreshold: qui ci si vuole fermare VICINO all'oggetto.")]
    public float objectArrivalThreshold = 0.6f;

    [Tooltip("Cartella dei file *.json del grafo (graph_snapshots)")]
    public string graphSnapshotsFolder =
        @"%USERPROFILE%\AppData\LocalLow\DefaultCompany\JaCaMoIntegration\graph_snapshots";

    // nodeId -> posizione mondo
    private readonly Dictionary<string, Vector3> nodePositions = new Dictionary<string, Vector3>();

    private string currentTargetNode = null;
    private string finalApproachLabel = null; // != null durante l'avvicinamento a un oggetto
    private Vector3 currentTargetPos;
    private bool isMoving = false;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        StartCoroutine(LoadNodePositionsWithRetry());
    }

    // La libreria chiama StartWalking() per il cammino "libero" (random/waypoint).
    // Questi agenti non vagano: qui non parte nessun wander.
    public override void StartWalking()
    {
        isStopped = false;
        if (agent != null) agent.isStopped = false;
    }

    public bool HasNode(string nodeId) => nodePositions.ContainsKey(nodeId);

    // --------------------------------------------------------
    // Chiamato dall'avatar quando arriva walk(goto, NodeId) da JaCaMo
    // --------------------------------------------------------
    public void GoToNode(string nodeId)
    {
        if (!nodePositions.TryGetValue(nodeId, out Vector3 targetPos))
        {
            Debug.LogWarning($"[TopologicalMovement] Nodo '{nodeId}' non trovato nelle posizioni caricate.");
            return;
        }
         if (TryResolveArtifactPosition(nodeId, out Vector3 artifactPos))
    {
        MoveToTarget(nodeId, artifactPos, true);
        return;
    }

    Debug.LogWarning($"[TopologicalMovement] Nodo/artefatto '{nodeId}' non trovato.");

        // Riattiva il movimento nel caso un targeted-walk verso un artefatto
        // avesse disabilitato il MovementModel.
        enabled = true;
        isStopped = false;
        if (agent != null) agent.isStopped = false;

        currentTargetNode = nodeId;
        currentTargetPos = targetPos;
        isMoving = true;
        agent.SetDestination(targetPos);
        Debug.Log($"[TopologicalMovement] Vado verso {nodeId} @ {targetPos}");
    }
    // Avvicinamento finale a una POSIZIONE libera (centro oggetto), non a un nodo.
    // 'label' è ciò che verrà notificato come reached(place, label) all'arrivo.
    public void GoToPosition(float x, float y, float z, string label)
    {
        Vector3 targetPos = new Vector3(x, y, z);

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            targetPos = hit.position;
        else
            Debug.LogWarning($"[TopologicalMovement] Posizione di '{label}' non proiettabile sul NavMesh, uso grezza.");

        enabled = true;
        isStopped = false;
        if (agent != null) agent.isStopped = false;

        currentTargetNode  = label;     // riusa la macchina di arrivo esistente
        currentTargetPos   = targetPos;
        finalApproachLabel = label;     // segnala che è un avvicinamento a oggetto
        isMoving = true;
        agent.SetDestination(targetPos);
        Debug.Log($"[TopologicalMovement] Avvicinamento a '{label}' @ {targetPos}");
    }
    private void MoveToTarget(string label, Vector3 targetPos, bool isArtifact)
{
    if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        targetPos = hit.position;

    enabled = true;
    isStopped = false;
    if (agent != null) agent.isStopped = false;

    currentTargetNode = label;
    currentTargetPos = targetPos;
    finalApproachLabel = isArtifact ? label : null;
    isMoving = true;

    agent.SetDestination(targetPos);

    Debug.Log(isArtifact
        ? $"[TopologicalMovement] Avvicinamento artefatto '{label}'"
        : $"[TopologicalMovement] Vado verso nodo '{label}'");
}
private bool TryResolveArtifactPosition(string artifactId, out Vector3 pos)
{
    GameObject obj = GameObject.Find(artifactId);

    if (obj != null)
    {
        pos = obj.transform.position;
        return true;
    }

    pos = Vector3.zero;
    return false;
}
    void Update()
{
    if (!isMoving || currentTargetNode == null) return;
    if (agent.pathPending) return;

    float dist = Vector3.Distance(transform.position, currentTargetPos);

    // oggetto -> raggio stretto; navigazione tra nodi -> raggio normale
    float threshold = (finalApproachLabel != null) ? objectArrivalThreshold : arrivalThreshold;

    bool passedThrough = dist <= threshold;
    bool fullyStopped  = agent.remainingDistance <= agent.stoppingDistance
                         && agent.velocity.sqrMagnitude < 0.01f;

    if (passedThrough || fullyStopped)
    {
        string reached = currentTargetNode;
        bool wasFinalApproach = finalApproachLabel != null;

        isMoving = false;
        currentTargetNode = null;
        finalApproachLabel = null;

        if (wasFinalApproach && agent != null) agent.isStopped = true; // fermo sull'oggetto

        var bridge = GetComponent<AgentJacamoBridge>();
        if (bridge != null)
        {
            if (wasFinalApproach)
                bridge.SendObjectReached(reached);     // solo reached(place, Artifact), NIENTE current_room
            else
                bridge.SendMovementCompleted(reached); // nodo: reached + current_room (invariato)
        }
        else
            Debug.LogWarning("[TopologicalMovement] Nessun AgentJacamoBridge: arrivo non notificato.");}
}

    // ========================================================
    // CARICAMENTO POSIZIONI NODI (identico al tuo VesnaMover)
    // ========================================================
    private IEnumerator LoadNodePositionsWithRetry()
    {
        while (nodePositions.Count == 0)
        {
            LoadNodePositions();
            if (nodePositions.Count > 0) break;
            Debug.Log("[TopologicalMovement] building.json non ancora disponibile, riprovo in 3s...");
            yield return new WaitForSeconds(3f);
        }
    }

    private void LoadNodePositions()
    {
        nodePositions.Clear();

        string folder = System.Environment.ExpandEnvironmentVariables(graphSnapshotsFolder);
        if (!Directory.Exists(folder))
        {
            Debug.Log($"[TopologicalMovement] Cartella grafo non ancora presente: {folder}");
            return;
        }

        // file .json più recente, escludendo target.json, pathfinder_state.json, *_artifacts.json
        string buildingFile = null;
        System.DateTime newest = System.DateTime.MinValue;

        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            string name = Path.GetFileName(file);
            if (name.Equals("target.json", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("pathfinder_state.json", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (name.EndsWith("_artifacts.json", System.StringComparison.OrdinalIgnoreCase)) continue;

            var mtime = File.GetLastWriteTimeUtc(file);
            if (mtime > newest)
            {
                newest = mtime;
                buildingFile = file;
            }
        }

        if (buildingFile == null)
        {
            Debug.Log($"[TopologicalMovement] Nessun building*.json ancora presente in {folder}");
            return;
        }

        Debug.Log($"[TopologicalMovement] Carico posizioni nodi da: {buildingFile}");

        string json = File.ReadAllText(buildingFile);
        JObject root = JObject.Parse(json);

        foreach (var floor in root["floors"])
        {
            foreach (var node in floor["nodes"])
            {
                string nodeType = node["nodeType"]?.ToString();
                float x = node["x"].Value<float>();
                float y = node["y"].Value<float>();
                float z = node["z"].Value<float>();
                Vector3 pos = new Vector3(x, y, z);

                if (nodeType == "CorridorPoleA" || nodeType == "CorridorPoleB")
                {
                    string nodeId = node["nodeId"]?.ToString();
                    string corridorId = node["corridorId"]?.ToString();

                    if (!string.IsNullOrEmpty(nodeId))
                        nodePositions[nodeId] = pos;

                    if (!string.IsNullOrEmpty(corridorId) && !nodePositions.ContainsKey(corridorId))
                        nodePositions[corridorId] = pos;
                }
                else
                {
                    string nodeId = node["nodeId"]?.ToString();
                    if (!string.IsNullOrEmpty(nodeId))
                        nodePositions[nodeId] = pos;
                }
            }
        }

        Debug.Log($"[TopologicalMovement] Posizioni nodi caricate: {nodePositions.Count}");

        // carica anche i nodi mancanti da pathfinder_state.json
        LoadFromPathfinderState();
    }

    private void LoadFromPathfinderState()
    {
        string path = Path.Combine(
            System.Environment.ExpandEnvironmentVariables(graphSnapshotsFolder),
            "pathfinder_state.json");
        if (!File.Exists(path)) return;

        var root = JObject.Parse(File.ReadAllText(path));
        foreach (var node in root["nodes"])
        {
            string nodeId = node[0]?.ToString();
            float x = node[3].Value<float>();
            float y = node[4].Value<float>();
            float z = node[5].Value<float>();
            if (!string.IsNullOrEmpty(nodeId) && !nodePositions.ContainsKey(nodeId))
                nodePositions[nodeId] = new Vector3(x, y, z);
        }
    }
}