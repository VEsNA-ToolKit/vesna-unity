using UnityEngine;

// ============================================================
// CorridorData.cs
// ============================================================
// Componente sul GameObject padre del corridoio.
//
// IDENTIFICAZIONE — drag & drop (come DoorVarcoScript):
//   Trascina nell'Inspector il GameObject che rappresenta
//   questo corridoio (la stanza/nodo nel grafo dell'edificio).
//   corridorId è derivato dal nome del GameObject, esattamente
//   come roomNameFront => roomFront.name in DoorVarcoScript.
//
//   Non scrivere mai corridorId a mano: trascina corridorNode.
// ============================================================

public class CorridorData : MonoBehaviour
{
    [Header("Identificazione")]
    [Tooltip("Trascina il GameObject di questo corridoio dall'Inspector.\n" +
             "L'ID viene letto dal nome del GameObject — non scrivere a mano.\n" +
             "Stesso pattern di roomFront/roomBack in DoorVarcoScript.")]
    public GameObject corridorNode;

    // ID derivato dal GameObject — sola lettura, mai assegnato a mano.
    // Se corridorNode non è assegnato, usa il nome del GameObject padre
    // come fallback (lo stesso comportamento di roomNameFront/Back).
    public string corridorId => corridorNode != null
        ? corridorNode.name
        : gameObject.name;

    // Lette all'Awake dai figli — sola lettura per gli esterni
    public Vector3 Pole1Position { get; private set; }
    public Vector3 Pole2Position { get; private set; }

    // Riferimenti ai poli — usati da ExplorationManager
    public CorridorPoleScript Pole1 { get; private set; }
    public CorridorPoleScript Pole2 { get; private set; }

    void Awake()
    {
        if (!Application.IsPlaying(gameObject)) return;
        ConfigureChildren();
    }

    private void ConfigureChildren()
    {
        var poles = GetComponentsInChildren<CorridorPoleScript>();
        if (poles.Length != 2)
        {
            Debug.LogError($"[CorridorData] {corridorId}: servono 2 poli, trovati {poles.Length}");
            return;
        }

        Pole1 = poles[0];
        Pole2 = poles[1];

        Pole1.corridorId = corridorId;
        Pole1.poleId     = "1";
        Pole2.corridorId = corridorId;
        Pole2.poleId     = "2";

        Pole1Position = Pole1.transform.position;
        Pole2Position = Pole2.transform.position;

        Debug.Log($"[CorridorData] {corridorId} configurato: P1={Pole1Position} P2={Pole2Position}");
    }

    // Aggiorna i nomi dei figli quando corridorNode cambia nell'Editor.
    // Chiamato da CorridorDataEditor al cambio del campo.
    public void OnCorridorIdChanged()
    {
        var poles = GetComponentsInChildren<CorridorPoleScript>();
        if (poles.Length > 0) { poles[0].gameObject.name = $"Pole_1_{corridorId}"; poles[0].corridorId = corridorId; }
        if (poles.Length > 1) { poles[1].gameObject.name = $"Pole_2_{corridorId}"; poles[1].corridorId = corridorId; }
    }
}