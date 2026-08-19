using System;
using System.Collections.Generic;

// ============================================================
// BuildingSnapshot.cs
// ============================================================
// DTO (Data Transfer Object) puro per lo snapshot dell'edificio.
//
// PRINCIPI GUIDA:
//   • Zero dipendenze da Unity (no MonoBehaviour, no Vector3, no
//     classi astratte) — serializzabile con Newtonsoft senza flags.
//   • Versionato: ogni campo aggiunto in futuro non rompe i file
//     salvati in precedenza (Newtonsoft ignora campi mancanti).
//   • Auto-contenuto: contiene TUTTO il necessario per ricostruire
//     lo stato del grafo E per ritrasmetterlo a JaCaMo, senza
//     bisogno del grafo vivo in memoria.
//   • Scalabile a N piani, N porte, N connettori.
//
// ESTENDERE IN FUTURO:
//   - Aggiungere campi in NodeSnapshot / EdgeSnapshot è sicuro
//     purché abbiano un valore di default (es. = false, = 0).
//   - Per cambiamenti strutturali, incrementa SCHEMA_VERSION e
//     aggiungi logica di migrazione in GraphPersistence.Migrate().
// ============================================================

[Serializable]
public class BuildingSnapshot
{
    // ── Metadati ────────────────────────────────────────────
    public const int SCHEMA_VERSION = 1;

    public int    schemaVersion = SCHEMA_VERSION;
    public string buildingId;           // identificatore edificio (es. "Edificio_A")
    public string capturedAt;           // DateTime.UtcNow.ToString("O") al momento del salvataggio
    public string unityVersion;         // Application.unityVersion
    
    // ── Dati per piano ──────────────────────────────────────
    // Ogni FloorSnapshot è indipendente: aggiungere un piano
    // non richiede modifiche al formato degli altri.
    public List<FloorSnapshot>          floors          = new List<FloorSnapshot>();

    // ── Link cross-floor (scale, ascensori, ecc.) ───────────
    public List<ConnectorLinkSnapshot>  connectorLinks  = new List<ConnectorLinkSnapshot>();

    // ── Distanze stanze ──────────────────────────────────────
    public List<RoomDistSnapshot>       roomDistances   = new List<RoomDistSnapshot>();
    public List<RoomObjectSnapshot>     discoveredObjects = new List<RoomObjectSnapshot>(); // ← aggiungi



}

// ─────────────────────────────────────────────────────────────
// SNAPSHOT DI UN SINGOLO PIANO
// ─────────────────────────────────────────────────────────────
[Serializable]
public class FloorSnapshot
{
    public int              floorIndex;
    public List<NodeSnapshot> nodes = new List<NodeSnapshot>();
    public List<EdgeSnapshot> edges = new List<EdgeSnapshot>();
}

// ─────────────────────────────────────────────────────────────
// NODO
// Unifica RoomNode e CorridorPoleNode in un unico DTO piatto.
// I campi corridorId e poleLabel sono validi solo se
// nodeType == "CorridorPoleA" o "CorridorPoleB".
// ─────────────────────────────────────────────────────────────
[Serializable]
public class NodeSnapshot
{
    public string nodeId;
    public string nodeType;             // NodeType.ToString()
    public float  x, y, z;             // position (no Vector3)
    public List<string> artifacts = new List<string>(); 
   

    // Solo per nodi polo corridoio
    public string corridorId;
    public string poleLabel;            // "A" o "B"
    public int wsPort;
}

// ─────────────────────────────────────────────────────────────
// ARCO
// ─────────────────────────────────────────────────────────────
[Serializable]
public class EdgeSnapshot
{
    public string edgeId;
    public string fromNodeId;
    public string toNodeId;             // null → "unknown"
    public string edgeType;             // EdgeType.ToString()
    public string edgeState;            // EdgeState.ToString()
    public string doorState;            // DoorState.ToString()
    
    public bool   isCorridorLink;
    public bool   isPhysical;          // ← da DoorVarcoScript.elementType (Door=true, Varco=false)
   // Campi solo corridoio — nullable
    public string side;
    public float? distance; // solo per Central
    public float? distFromA;
    public float? distFromB;
    public int?   orderFW;
    public int?   orderBW;
    public int?   explorationOrder;
    public float  x, y, z;             // position (no Vector3)
    public int wsPort; // porta WebSocket del bridge corrispondente
}

// ─────────────────────────────────────────────────────────────
// LINK CROSS-FLOOR
// ─────────────────────────────────────────────────────────────
[Serializable]
public class ConnectorLinkSnapshot
{
    public string connectorId;
    public int    floorA;
    public int    floorB;
    public bool   bidirectional;
    public float  posAx, posAy, posAz;  // posizione fisica lato floorA
    public float  posBx, posBy, posBz;  // posizione fisica lato floorB
}

// ─────────────────────────────────────────────────────────────
// DISTANZA TRA DUE PORTE
// ─────────────────────────────────────────────────────────────
[Serializable]
public class RoomDistSnapshot
{
    public string roomA;
    public string roomB;
    public int    floor;
    public float  distance;
    public string doorName;
}

// ─────────────────────────────────────────────────────────────
// ARTEFATTO SCOPERTO A RUNTIME
// Salvato quando il cono visivo vede l'oggetto per la prima
// volta. roomId è il nome della stanza (es. "room_lobby").
// Se l'oggetto viene successivamente visto in una stanza
// diversa, roomId viene aggiornato tramite UpdateDiscoveredObject().
// ─────────────────────────────────────────────────────────────

[Serializable]
public class RoomObjectSnapshot
{
    public string artifactId;       // ID univoco (es. "chair_lobby")
    public string roomId;           // stanza in cui è stato visto l'ultima volta
    public string artifactType;
    public float  x, y, z;         // posizione 3D al momento della scoperta
    public int    wsPort;           // porta WebSocket del suo RoomObjectArtifact.java
}