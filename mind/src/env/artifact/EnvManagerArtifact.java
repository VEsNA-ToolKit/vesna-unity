package artifact;
import artifact.lib.maselements.AbstractMasElementArtifact;
import artifact.lib.model.WsMessage;
import artifact.lib.utils.ObjectMapperUtils;
import cartago.OPERATION;
import cartago.OpFeedbackParam;
import com.fasterxml.jackson.core.type.TypeReference;
import jason.asSyntax.ASSyntax;
import jason.asSyntax.ListTerm;
import org.json.JSONArray;
import org.json.JSONObject;
import pathfinding.AStarPathfinder;

import java.util.ArrayList;
import java.util.List;

public class EnvManagerArtifact extends AbstractMasElementArtifact {

    private final List<Object[]> rawNodes    = new ArrayList<>();
    private final List<Object[]> rawEdges    = new ArrayList<>();
    private final List<Object[]> rawDoorDist = new ArrayList<>();
    private final List<Object[]> rawObjects  = new ArrayList<>();
    private AStarPathfinder cachedPathfinder = null;
    private static final String STATE_FILE =
            System.getProperty("user.home")
            + "\\AppData\\LocalLow\\DefaultCompany\\JaCaMoIntegration\\graph_snapshots\\pathfinder_state.json";

    // -------------------------------------------------------------------
    // NUOVO init senza WebSocket: l'envManager non ha bisogno di WS,
    // solo di caricare il grafo da disco se esiste.
    // Nel .jcm: artifact envManager: artifact.EnvManagerArtifact("envManager")
    // -------------------------------------------------------------------
  @OPERATION
protected void init(String artifactName) {
    defineObsProperty("graphReady", false);
    System.out.println("[envManager] inizializzato (no WebSocket).");
    if (loadFromDisk()) {
         cachedPathfinder = buildGraph(); 
        getObsProperty("graphReady").updateValue(true);
        signal("graphReady");   // <-- AGGIUNTO: notifica vesna anche se il grafo viene da disco
        System.out.println("[envManager] Grafo caricato da disco (" + rawNodes.size() + " nodi).");
    }
}

    // -------------------------------------------------------------------
    // Operazioni env manager (retrieval artefatti da Unity)
    // -------------------------------------------------------------------
    @OPERATION
    public void retrieveArtifactsByType(String artifactType, String agentName){
        try{
            lock.lock();
            WsMessage wsMessage = prepareMessage("retrieveArtifacts", "all_artifact_by_type", agentName, artifactType);
            writeLog("Sending message: " + ObjectMapperUtils.convertIntoJsonString(wsMessage));
            send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
        }finally {
            lock.unlock();
        }
    }

    @OPERATION
    public void retrieveAllArtifacts(String agentName){
        try{
            lock.lock();
            WsMessage wsMessage = prepareMessage("retrieveArtifacts", "all_artifact", agentName, null);
            send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
        }finally {
            lock.unlock();
        }
    }

    @OPERATION
    public void retrieveNearestArtifactByType(String artifactType, String agentName){
        try{
            lock.lock();
            WsMessage wsMessage = prepareMessage("retrieveArtifacts", "nearest", agentName, artifactType);
            writeLog("Sending message: " + ObjectMapperUtils.convertIntoJsonString(wsMessage));
            send(ObjectMapperUtils.convertIntoJsonString(wsMessage));
        }finally {
            lock.unlock();
        }
    }

    private static WsMessage prepareMessage(String messageType, String messagePayload, String agentName, String param) {
        WsMessage wsMessage = new WsMessage();
        wsMessage.setMessageType(messageType);
        wsMessage.setMessagePayload(messagePayload);
        wsMessage.setAgentName(agentName);
        wsMessage.setParam(param);
        return wsMessage;
    }

    @Override
    public void onConnectionEstablished(){
        super.onConnectionEstablished();
        execInternalOp("signalAllAgents", "connection_env_leader_established", true);
    }

    @Override
    public void onMessageReceived(String message) {
        writeLog("Message received from Unity: " + message);
        try{
            lock.lock();
            JSONObject messageJson = new JSONObject(message);
            String type = messageJson.getString("type");
            String receiver = messageJson.getString("receiver");

            if (!type.equals("arts_info"))
                throw new Exception("Wrong message type: " + type + ". Expected 'arts_info'.");

            List<String> artifactNames = messageJson.getJSONObject("data")
                    .getJSONArray("names")
                    .toList()
                    .stream()
                    .map(Object::toString)
                    .toList();

            execInternalOp("signalAgent", receiver, "artifact_names", artifactNames.toArray());
        }catch (Exception e){
            e.printStackTrace();
        }finally {
            lock.unlock();
        }
    }

    // ===================================================================
    // PATHFINDING
    // ===================================================================

    // -------------------------------------------------------------------
    // Ricezione atomica del grafo completo.
    // Jason/CArtAgO converte le liste AgentSpeak in Object[], non in
    // String[]/int[]/double[]: per questo la firma usa Object[].
    // -------------------------------------------------------------------
    @OPERATION
    public synchronized void setCompleteGraph(
            Object[] ids, Object[] types, Object[] floors,
            Object[] xs, Object[] ys, Object[] zs,
            Object[] froms, Object[] tos, Object[] edgeTypes, Object[] dirs,
            Object[] distAs, Object[] distBs,
            Object[] roomAs, Object[] roomBs, Object[] doorDists,
            Object[] artifactIds, Object[] roomIds) {

        try {
            System.out.println("[envManager] Inizio setCompleteGraph");

            requireSameLength("nodes", ids, types, floors, xs, ys, zs);
            requireSameLength("edges", froms, tos, edgeTypes, dirs, distAs, distBs);
            requireSameLength("door distances", roomAs, roomBs, doorDists);
            requireSameLength("objects", artifactIds, roomIds);

            System.out.println("[envManager] Ricevuti "
                    + ids.length + " nodi, "
                    + froms.length + " archi, "
                    + roomAs.length + " distanze porta, "
                    + artifactIds.length + " oggetti.");

            rawNodes.clear();
            rawEdges.clear();
            rawDoorDist.clear();
            rawObjects.clear();
            cachedPathfinder = null;

            for (int i = 0; i < ids.length; i++) {
                try {
                    rawNodes.add(new Object[]{
                            cleanString(ids[i]), cleanString(types[i]), toInt(floors[i]),
                            toDouble(xs[i]), toDouble(ys[i]), toDouble(zs[i])
                    });
                } catch (Exception ex) {
                    throw new IllegalArgumentException(
                            "Errore nel nodo indice " + i
                                    + ": id=" + ids[i]
                                    + ", type=" + types[i]
                                    + ", floor=" + floors[i]
                                    + ", x=" + xs[i]
                                    + ", y=" + ys[i]
                                    + ", z=" + zs[i], ex);
                }
            }
            System.out.println("[envManager] Nodi convertiti correttamente.");

            for (int i = 0; i < froms.length; i++) {
                try {
                    rawEdges.add(new Object[]{
                            cleanString(froms[i]), cleanString(tos[i]),
                            cleanString(edgeTypes[i]), cleanString(dirs[i]),
                            toDouble(distAs[i]), toDouble(distBs[i])
                    });
                } catch (Exception ex) {
                    throw new IllegalArgumentException(
                            "Errore nell'arco indice " + i
                                    + ": from=" + froms[i]
                                    + ", to=" + tos[i]
                                    + ", type=" + edgeTypes[i]
                                    + ", dir=" + dirs[i]
                                    + ", distA=" + distAs[i]
                                    + ", distB=" + distBs[i], ex);
                }
            }
            System.out.println("[envManager] Archi convertiti correttamente.");

            for (int i = 0; i < roomAs.length; i++) {
                rawDoorDist.add(new Object[]{
                        cleanString(roomAs[i]), cleanString(roomBs[i]),
                        toDouble(doorDists[i])
                });
            }

            for (int i = 0; i < artifactIds.length; i++) {
                rawObjects.add(new Object[]{
                        cleanString(artifactIds[i]), cleanString(roomIds[i])
                });
            }

            System.out.println("[envManager] Costruzione del grafo A*...");
            cachedPathfinder = buildGraph();
            System.out.println("[envManager] Grafo A* costruito correttamente.");

            saveToDisk();
            getObsProperty("graphReady").updateValue(true);
            signal("graphReady");

            System.out.println("[envManager] Grafo completo ricevuto: "
                    + rawNodes.size() + " nodi, "
                    + rawEdges.size() + " archi, "
                    + rawDoorDist.size() + " distanze porta, "
                    + rawObjects.size() + " oggetti.");

        } catch (Exception ex) {
            System.err.println("[envManager] ERRORE setCompleteGraph:");
            ex.printStackTrace();
            failed("Errore durante setCompleteGraph: "
                    + ex.getClass().getSimpleName() + " - " + ex.getMessage());
        }
    }

    private static void requireSameLength(String group, Object[] first, Object[]... others) {
        if (first == null) {
            throw new IllegalArgumentException("Lista nulla per " + group);
        }
        int expected = first.length;
        for (Object[] values : others) {
            if (values == null || values.length != expected) {
                throw new IllegalArgumentException(
                        "Lunghezze non coerenti per " + group
                                + ": attesi " + expected
                                + ", ricevuti " + (values == null ? "null" : values.length));
            }
        }
    }

    private static String cleanString(Object value) {
        if (value == null) {
            throw new IllegalArgumentException("Valore stringa nullo");
        }

        String result = value.toString();
        if (result.length() >= 2
                && result.startsWith("\"")
                && result.endsWith("\"")) {
            result = result.substring(1, result.length() - 1);
        }
        return result;
    }

    private static int toInt(Object value) {
        if (value == null) {
            throw new IllegalArgumentException("Valore intero nullo");
        }
        if (value instanceof Number) {
            return ((Number) value).intValue();
        }
        return (int) Double.parseDouble(cleanString(value));
    }

    private static double toDouble(Object value) {
        if (value == null) {
            throw new IllegalArgumentException("Valore double nullo");
        }
        if (value instanceof Number) {
            return ((Number) value).doubleValue();
        }
        return Double.parseDouble(cleanString(value));
    }

    @OPERATION
    public void computePath(String startId, String goalId,
                            OpFeedbackParam<String> resolvedGoalOut,
                            OpFeedbackParam<Object>  pathOut,
                            OpFeedbackParam<Double>  costOut) {
        AStarPathfinder pf = (cachedPathfinder != null) ? cachedPathfinder : buildGraph();

        String resolvedGoal = goalId;
        if (!pf.getNodeIds().contains(goalId)) {
            for (Object[] o : rawObjects) {
                if (o[0].equals(goalId)) { resolvedGoal = (String) o[1]; break; }
            }
        }

        List<String> path;
        double cost;

        if (pf.getNodeIds().contains(resolvedGoal)) {
            path = pf.computePath(startId, resolvedGoal);
            cost = pf.pathCost(path);
        } else {
            String poleA = resolvedGoal + "_A";
            String poleB = resolvedGoal + "_B";

            List<String> pathA = pf.getNodeIds().contains(poleA)
                    ? pf.computePath(startId, poleA) : List.of();
            double costA = pathA.isEmpty() ? Double.MAX_VALUE : pf.pathCost(pathA);

            List<String> pathB = pf.getNodeIds().contains(poleB)
                    ? pf.computePath(startId, poleB) : List.of();
            double costB = pathB.isEmpty() ? Double.MAX_VALUE : pf.pathCost(pathB);

            if (costA == Double.MAX_VALUE && costB == Double.MAX_VALUE) {
                path = List.of(); cost = 0.0;
            } else if (costA <= costB) {
                path = pathA; cost = costA; resolvedGoal = poleA;
            } else {
                path = pathB; cost = costB; resolvedGoal = poleB;
            }
        }

        pf.printPath(path);

        ListTerm pathList = ASSyntax.createList();
        for (String nodeId : path) {
            pathList.add(ASSyntax.createString(nodeId));
        }

        resolvedGoalOut.set(resolvedGoal);
        pathOut.set(pathList);
        costOut.set(cost);
    }

    private AStarPathfinder buildGraph() {
        AStarPathfinder pf = new AStarPathfinder();
        List<String> nodeIds = new ArrayList<>();

        for (Object[] n : rawNodes) {
            String id = (String) n[0];
            pf.addNode(
                    id,
                    (String) n[1],
                    ((Number) n[2]).intValue(),
                    ((Number) n[3]).doubleValue(),
                    ((Number) n[4]).doubleValue(),
                    ((Number) n[5]).doubleValue()
            );
            nodeIds.add(id);
        }

        for (int i = 0; i < rawEdges.size(); i++) {
            Object[] e = rawEdges.get(i);

            String from = (String) e[0];
            String to = (String) e[1];
            String edgeType = (String) e[2];
            String dir = (String) e[3];
            double distA = ((Number) e[4]).doubleValue();
            double distB = ((Number) e[5]).doubleValue();

            if (!nodeIds.contains(from)) {
                throw new IllegalArgumentException(
                        "Arco " + i + ": nodo FROM inesistente: " + from);
            }
            if (!nodeIds.contains(to)) {
                throw new IllegalArgumentException(
                        "Arco " + i + ": nodo TO inesistente: " + to);
            }

            double weight;
            switch (dir) {
                case "fw"      -> weight = distA;
                case "bw"      -> weight = distB;
                case "seg"     -> weight = distA > 0 ? distA : distB;
                case "central" -> weight = distA;
                case "none"    -> weight = getDoorDist(from, to);
                default -> throw new IllegalArgumentException(
                        "Direzione non riconosciuta nell'arco " + i
                                + ": " + dir + " (" + from + " -> " + to + ")");
            }

            pf.addEdge(from, to, weight, edgeType);
        }

        return pf;
    }

    private double getDoorDist(String from, String to) {
        for (Object[] d : rawDoorDist) {
            String roomA = (String) d[0];
            String roomB = (String) d[1];
            if ((roomA.equals(from) && roomB.equals(to)) ||
                (roomA.equals(to)   && roomB.equals(from))) {
                return (double) d[2];
            }
        }
        return 0.0;
    }

    private void saveToDisk() {
        try {
            JSONObject root = new JSONObject();

            JSONArray nodesArr = new JSONArray();
            for (Object[] n : rawNodes) {
                JSONArray a = new JSONArray();
                for (Object v : n) a.put(v);
                nodesArr.put(a);
            }

            JSONArray edgesArr = new JSONArray();
            for (Object[] e : rawEdges) {
                JSONArray a = new JSONArray();
                for (Object v : e) a.put(v);
                edgesArr.put(a);
            }

            JSONArray doorDistArr = new JSONArray();
            for (Object[] d : rawDoorDist) {
                JSONArray a = new JSONArray();
                for (Object v : d) a.put(v);
                doorDistArr.put(a);
            }

            JSONArray objectsArr = new JSONArray();
            for (Object[] o : rawObjects) {
                JSONArray a = new JSONArray();
                for (Object v : o) a.put(v);
                objectsArr.put(a);
            }

            root.put("nodes", nodesArr);
            root.put("edges", edgesArr);
            root.put("doorDist", doorDistArr);
            root.put("objects", objectsArr);

            java.io.File f = new java.io.File(STATE_FILE);
            f.getParentFile().mkdirs();
            java.nio.file.Files.write(f.toPath(), root.toString(2).getBytes("UTF-8"));
            System.out.println("[envManager] Stato salvato in " + STATE_FILE);
        } catch (Exception e) {
            System.out.println("[envManager] Errore salvataggio: " + e.getMessage());
        }
    }

    private boolean loadFromDisk() {
        try {
            java.io.File f = new java.io.File(STATE_FILE);
            if (!f.exists()) return false;

            String content = new String(java.nio.file.Files.readAllBytes(f.toPath()), "UTF-8");
            JSONObject root = new JSONObject(content);

            JSONArray nodesArr = root.getJSONArray("nodes");
            for (int i = 0; i < nodesArr.length(); i++) {
                JSONArray a = nodesArr.getJSONArray(i);
                rawNodes.add(new Object[]{
                    a.getString(0), a.getString(1), a.getInt(2),
                    a.getDouble(3), a.getDouble(4), a.getDouble(5)
                });
            }

            JSONArray edgesArr = root.getJSONArray("edges");
            for (int i = 0; i < edgesArr.length(); i++) {
                JSONArray a = edgesArr.getJSONArray(i);
                rawEdges.add(new Object[]{
                    a.getString(0), a.getString(1), a.getString(2),
                    a.getString(3), a.getDouble(4), a.getDouble(5)
                });
            }

            JSONArray doorDistArr = root.getJSONArray("doorDist");
            for (int i = 0; i < doorDistArr.length(); i++) {
                JSONArray a = doorDistArr.getJSONArray(i);
                rawDoorDist.add(new Object[]{
                    a.getString(0), a.getString(1), a.getDouble(2)
                });
            }

            JSONArray objectsArr = root.getJSONArray("objects");
            for (int i = 0; i < objectsArr.length(); i++) {
                JSONArray a = objectsArr.getJSONArray(i);
                rawObjects.add(new Object[]{ a.getString(0), a.getString(1) });
            }

            return !rawNodes.isEmpty();
        } catch (Exception e) {
            System.out.println("[envManager] Errore caricamento: " + e.getMessage());
            return false;
        }
    }
}