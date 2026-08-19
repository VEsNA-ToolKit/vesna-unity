package pathfinding;

import java.util.*;

/**
 * AStarPathfinder.java
 *
 * Implementazione dell'algoritmo A* per il calcolo del cammino minimo
 * sul grafo topologico dell'edificio costruito durante l'esplorazione Unity.
 *
 * GRAFO:
 *   Nodi  → stanze (Room) e poli corridoio (CorridorPoleA/B)
 *   Archi → porte (DoorFW/BW), varchi (Segment), segmenti corridoio (Central)
 *   Pesi  → distanze NavMesh calcolate da Unity (distFromA, distFromB)
 *
 * EURISTICA:
 *   Distanza euclidea 3D tra nodo corrente e nodo destinazione.
 *   Ammissibile perché la distanza euclidea ≤ distanza NavMesh reale.
 *
 * DESTINAZIONE:
 *   Può essere un nodeId (stanza) oppure un artifactId (oggetto nella stanza).
 *   Nel caso di artefatto, il goal è la stanza in cui si trova l'artefatto
 *   (recuperata dalla belief new_object).
 */
public class AStarPathfinder {

    // -----------------------------------------------------------------------
    // DTO interni
    // -----------------------------------------------------------------------

    /** Rappresenta un nodo del grafo topologico. */
    public static class NodeData {
        public final String id;
        public final String type;       // Room, CorridorPoleA, CorridorPoleB
        public final int    floor;
        public final double x, y, z;

        public NodeData(String id, String type, int floor,
                        double x, double y, double z) {
            this.id    = id;
            this.type  = type;
            this.floor = floor;
            this.x     = x;
            this.y     = y;
            this.z     = z;
        }
    }

    /** Rappresenta un arco pesato del grafo. */
    public static class EdgeData {
        public final String from;
        public final String to;
        public final double weight;     // distanza NavMesh in metri
        public final String edgeType;   // DoorFW, DoorBW, Segment, Central

        public EdgeData(String from, String to,
                        double weight, String edgeType) {
            this.from     = from;
            this.to       = to;
            this.weight   = weight;
            this.edgeType = edgeType;
        }
    }

    /** Nodo interno A* con f = g + h. */
    private static class AStarNode implements Comparable<AStarNode> {
        final String id;
        double g;   // costo dal nodo di partenza
        double h;   // stima euristica al goal
        double f;   // f = g + h
        AStarNode parent;

        AStarNode(String id, double g, double h, AStarNode parent) {
            this.id     = id;
            this.g      = g;
            this.h      = h;
            this.f      = g + h;
            this.parent = parent;
        }

        @Override
        public int compareTo(AStarNode other) {
            return Double.compare(this.f, other.f);
        }
    }

    // -----------------------------------------------------------------------
    // Struttura del grafo
    // -----------------------------------------------------------------------

    /** Mappa nodeId → dati del nodo (coordinate, tipo, piano). */
    private final Map<String, NodeData> nodes = new HashMap<>();

    /** Lista di adiacenza: nodeId → lista di archi uscenti. */
    private final Map<String, List<EdgeData>> adjacency = new HashMap<>();

    // -----------------------------------------------------------------------
    // Costruzione del grafo
    // -----------------------------------------------------------------------

    /**
     * Aggiunge un nodo al grafo.
     * Chiamato per ogni credenza node(...) ricevuta da Unity.
     */
    public void addNode(String id, String type, int floor,
                        double x, double y, double z) {
        nodes.put(id, new NodeData(id, type, floor, x, y, z));
        adjacency.putIfAbsent(id, new ArrayList<>());
    }

    /**
     * Aggiunge un arco al grafo.
     * Chiamato per ogni credenza edge(...) ricevuta da Unity.
     *
     * @param from      nodo sorgente
     * @param to        nodo destinazione
     * @param weight    distanza NavMesh (distFromA o distFromB)
     * @param edgeType  tipo arco (DoorFW, DoorBW, Segment, Central)
     */
    public void addEdge(String from, String to, double weight, String edgeType) {
    // Fallback euclideo se peso è 0 o negativo
    if (weight <= 0 && nodes.containsKey(from) && nodes.containsKey(to)) {
        NodeData a = nodes.get(from);
        NodeData b = nodes.get(to);
        double dx = a.x - b.x;
        double dy = a.y - b.y;
        double dz = a.z - b.z;
        weight = Math.sqrt(dx*dx + dy*dy + dz*dz);
    }
    if (weight <= 0) return;

    // Aggiungi arco diretto
    adjacency.putIfAbsent(from, new ArrayList<>());
    adjacency.get(from).add(new EdgeData(from, to, weight, edgeType));
    // ← Aggiungi arco inverso automaticamente
    adjacency.putIfAbsent(to, new ArrayList<>());
    adjacency.get(to).add(new EdgeData(to, from, weight, edgeType));
}

    // -----------------------------------------------------------------------
    // Algoritmo A*
    // -----------------------------------------------------------------------

    /**
     * Calcola il cammino minimo da start a goal usando A*.
     *
     * @param startId   nodeId del nodo di partenza
     * @param goalId    nodeId del nodo destinazione
     * @return lista ordinata di nodeId che compongono il percorso,
     *         oppure lista vuota se non esiste un percorso
     */
    public List<String> computePath(String startId, String goalId) {
        if (!nodes.containsKey(startId) || !nodes.containsKey(goalId)) {
            System.out.println("[A*] Nodo non trovato: " + startId + " → " + goalId);
            return Collections.emptyList();
        }

        if (startId.equals(goalId)) {
            return Collections.singletonList(startId);
        }

        NodeData goalNode = nodes.get(goalId);

        // Open set — coda con priorità ordinata per f = g + h
        PriorityQueue<AStarNode> openSet = new PriorityQueue<>();

        // Mappa nodeId → miglior costo g trovato finora
        Map<String, Double> bestG = new HashMap<>();

        // Closed set — nodi già espansi
        Set<String> closedSet = new HashSet<>();

        // Inizializza con il nodo di partenza
        double hStart = heuristic(nodes.get(startId), goalNode);
        openSet.add(new AStarNode(startId, 0.0, hStart, null));
        bestG.put(startId, 0.0);

        while (!openSet.isEmpty()) {
            AStarNode current = openSet.poll();

            // Goal raggiunto — ricostruisci il percorso
            if (current.id.equals(goalId)) {
                return reconstructPath(current);
            }

            // Nodo già espanso con costo migliore → skip
            if (closedSet.contains(current.id)) continue;
            closedSet.add(current.id);

            // Espandi i vicini
            List<EdgeData> edges = adjacency.getOrDefault(current.id, Collections.emptyList());
            for (EdgeData edge : edges) {
                if (closedSet.contains(edge.to)) continue;
                if (!nodes.containsKey(edge.to))  continue;

                double tentativeG = current.g + edge.weight;

                // Aggiorna solo se troviamo un percorso migliore
                if (tentativeG < bestG.getOrDefault(edge.to, Double.MAX_VALUE)) {
                    bestG.put(edge.to, tentativeG);
                    double h = heuristic(nodes.get(edge.to), goalNode);
                    openSet.add(new AStarNode(edge.to, tentativeG, h, current));
                }
            }
        }

        // Nessun percorso trovato
        System.out.println("[A*] Nessun percorso trovato da " + startId + " a " + goalId);
        return Collections.emptyList();
    }

    // -----------------------------------------------------------------------
    // Euristica — distanza euclidea 3D
    // -----------------------------------------------------------------------

    /**
     * Stima ammissibile del costo rimanente.
     * Distanza euclidea 3D tra nodo corrente e goal.
     * È ammissibile perché distEuclid ≤ distNavMesh sempre.
     */
    private double heuristic(NodeData from, NodeData to) {
        double dx = from.x - to.x;
        double dy = from.y - to.y;
        double dz = from.z - to.z;
        return Math.sqrt(dx * dx + dy * dy + dz * dz);
    }

    // -----------------------------------------------------------------------
    // Ricostruzione del percorso
    // -----------------------------------------------------------------------

    /**
     * Risale la catena di parent dal goal fino allo start
     * e restituisce il percorso in ordine start → goal.
     */
    private List<String> reconstructPath(AStarNode goal) {
        List<String> path = new ArrayList<>();
        AStarNode current = goal;
        while (current != null) {
            path.add(0, current.id); // inserisci in testa
            current = current.parent;
        }
        return path;
    }

    // -----------------------------------------------------------------------
    // Utility
    // -----------------------------------------------------------------------

    /**
     * Restituisce tutti i nodeId presenti nel grafo.
     * Utile per debug o per verificare che il grafo sia stato costruito.
     */
    public Set<String> getNodeIds() {
        return Collections.unmodifiableSet(nodes.keySet());
    }

    /**
     * Restituisce i dati di un nodo dato il suo ID.
     */
    public NodeData getNode(String id) {
        return nodes.get(id);
    }

    /**
     * Calcola il costo totale di un percorso dato.
     * Utile per stampare la distanza totale del percorso trovato.
     */
    public double pathCost(List<String> path) {
        double total = 0.0;
        for (int i = 0; i < path.size() - 1; i++) {
            String from = path.get(i);
            String to   = path.get(i + 1);
            List<EdgeData> edges = adjacency.getOrDefault(from, Collections.emptyList());
            for (EdgeData e : edges) {
                if (e.to.equals(to)) {
                    total += e.weight;
                    break;
                }
            }
        }
        return total;
    }

    /**
     * Stampa il percorso in formato leggibile.
     */
    public void printPath(List<String> path) {
        if (path.isEmpty()) {
            System.out.println("[A*] Nessun percorso.");
            return;
        }
        System.out.println("[A*] Percorso (" + path.size() + " nodi, " +
                           String.format("%.2f", pathCost(path)) + "m):");
        for (int i = 0; i < path.size(); i++) {
            System.out.println("  " + (i + 1) + ". " + path.get(i));
        }
    }

    public List<EdgeData> getAdjacency(String nodeId) {
    return adjacency.getOrDefault(nodeId, Collections.emptyList());
}

    /**
     * Riempie un JSONObject con i dati del nodo (type, floor, x, y, z).
     * Usato da PathfinderArtifact per serializzare lo stato su disco.
     */
    public void fillNodeJson(String id, org.json.JSONObject out) {
        NodeData n = nodes.get(id);
        if (n == null) return;
        out.put("type", n.type);
        out.put("floor", n.floor);
        out.put("x", n.x);
        out.put("y", n.y);
        out.put("z", n.z);
    }

    /**
     * Restituisce tutti gli archi del grafo come array {from, to, weight, edgeType}.
     * Nota: addEdge aggiunge sempre l'arco in entrambe le direzioni, quindi
     * questo metodo ritorna anche l'inverso (ridondanza accettabile per
     * la persistenza: ricaricandolo, addEdge raddoppierebbe ancora —
     * ma il grafo resta corretto, solo con archi duplicati equivalenti).
     */
    public List<Object[]> getAllEdges() {
        List<Object[]> result = new ArrayList<>();
        for (List<EdgeData> edges : adjacency.values()) {
            for (EdgeData e : edges) {
                result.add(new Object[]{ e.from, e.to, e.weight, e.edgeType });
            }
        }
        return result;
    }
}