using System.Collections.Generic;
using UnityEngine;

// ============================================================
// BuildingGraph.cs — MonoBehaviour (in scena, drag & drop)
// ============================================================
// Mettilo su un GameObject in scena (es. "BuildingConfig").
// Trascina tutto dall'Inspector — niente stringhe a mano.
// ============================================================

public class BuildingGraph : MonoBehaviour
{
    [Header("Piani dell'edificio")]
    public List<FloorNode> floors = new List<FloorNode>();

    [Header("Connettori verticali (scale, ascensori)")]
    public List<VerticalConnector> connectors = new List<VerticalConnector>();

    public FloorNode GetFloor(int floorIndex)
        => floors.Find(f => f.floorIndex == floorIndex);

    public List<VerticalConnector> GetConnectors(int fromFloor, int toFloor)
        => connectors.FindAll(c =>
            (c.floorFrom == fromFloor && c.floorTo == toFloor) ||
            (c.bidirectional && c.floorFrom == toFloor && c.floorTo == fromFloor));

    public FloorNode GetNextFloor(int currentFloorIndex)
    {
        int pos = floors.FindIndex(f => f.floorIndex == currentFloorIndex);
        if (pos < 0 || pos >= floors.Count - 1) return null;
        return floors[pos + 1];
    }

    public List<FloorNode> GetUnexploredFloors()
        => floors.FindAll(f => !f.explored);
}

// ============================================================
// FLOOR NODE
// ============================================================
[System.Serializable]
public class FloorNode
{
    [Tooltip("Indice numerico del piano (0, 1, 2, ...)")]
    public int floorIndex;

    [Tooltip("Nome leggibile (es. 'Piano Terra')")]
    public string floorName;

    [Tooltip("GameObject da cui l'agente parte su questo piano.\nTrascina il GO dall'Inspector.")]
    public GameObject spawnObject;

    public Vector3 spawnPosition => spawnObject != null
        ? spawnObject.transform.position
        : Vector3.zero;

    [Tooltip("Stanze/corridoi di questo piano.\nTrascina i GO dall'Inspector.")]
    public List<GameObject> roomObjects = new List<GameObject>();

    public List<string> roomIds
    {
        get
        {
            var ids = new List<string>();
            foreach (var r in roomObjects)
                if (r != null) ids.Add(r.name);
            return ids;
        }
    }

    [HideInInspector] public bool explored = false;
}

// ============================================================
// VERTICAL CONNECTOR
// ============================================================
[System.Serializable]
public class VerticalConnector
{
    public enum ConnectorType { Stairs, Elevator }

    public ConnectorType type;

    [Tooltip("Trascina il GameObject della scala/ascensore dall'Inspector.")]
    public GameObject idObject;
    public string id => idObject != null ? idObject.name : string.Empty;// MESSO COSì PERCHè SE NO DOVEVAMO CAMBIARE TUTTO IL CODICE 

    public int   floorFrom;
    public int   floorTo;
    public bool  bidirectional = true;
    public float costUp   = 2.0f;
    public float costDown = 1.0f;

    [Tooltip("GameObject INIZIO scala/ascensore.\nTrascina il GO dall'Inspector.")]
    public GameObject triggerStartObject;

    [Tooltip("GameObject FINE scala/ascensore.\nTrascina il GO dall'Inspector.")]
    public GameObject triggerEndObject;

    public Vector3 triggerStartPosition => triggerStartObject != null
        ? triggerStartObject.transform.position
        : Vector3.zero;

    public Vector3 triggerEndPosition => triggerEndObject != null
        ? triggerEndObject.transform.position
        : Vector3.zero;

    [HideInInspector] public bool agentNearby = false;
}