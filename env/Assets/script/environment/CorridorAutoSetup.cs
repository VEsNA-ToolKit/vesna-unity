#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ============================================================
// CorridorAutoSetup.cs  — Editor Only
// ============================================================
// Custom Inspector per CorridorData.
//
// Mostra un ObjectField drag & drop per corridorNode
// (stesso pattern di roomFront/roomBack in DoorVarcoScript).
// corridorId è derivato automaticamente dal nome del GameObject
// trascinato — nessuna stringa da scrivere a mano.
// ============================================================

[CustomEditor(typeof(CorridorData))]
public class CorridorDataEditor : Editor
{
    private GameObject previousNode;

    void OnEnable()
    {
        previousNode = ((CorridorData)target).corridorNode;
    }

    public override void OnInspectorGUI()
    {
        var corridor = (CorridorData)target;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Corridoio", EditorStyles.boldLabel);

        // ── Campo drag & drop (come roomFront/roomBack) ──────────────────
        EditorGUI.BeginChangeCheck();
        var newNode = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent(
                "Corridor Node",
                "Trascina qui il GameObject del corridoio.\n" +
                "L'ID viene letto dal suo nome — stesso pattern di DoorVarcoScript."),
            corridor.corridorNode,
            typeof(GameObject),
            allowSceneObjects: true);

        if (EditorGUI.EndChangeCheck())
        {
            // Controlla che il nome non sia già usato da un altro corridoio
            if (newNode != null && IsNodeTaken(newNode, corridor))
            {
                EditorUtility.DisplayDialog("Node già usato",
                    $"'{newNode.name}' è già usato da un altro CorridorData.", "OK");
            }
            else
            {
                Undo.RecordObject(corridor, "Assign Corridor Node");
                corridor.corridorNode = newNode;
                previousNode          = newNode;
                corridor.OnCorridorIdChanged();
                EditorUtility.SetDirty(corridor);
            }
        }

        // ── ID derivato — sola lettura ───────────────────────────────────
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(
            new GUIContent("Corridor ID (auto)", "Derivato dal nome di Corridor Node."),
            corridor.corridorId);
        EditorGUI.EndDisabledGroup();

        if (corridor.corridorNode == null)
            EditorGUILayout.HelpBox(
                "Trascina un GameObject in 'Corridor Node' per assegnare l'ID.",
                MessageType.Warning);

        // ── Stato poli ───────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        var poles = corridor.GetComponentsInChildren<CorridorPoleScript>();
        EditorGUILayout.LabelField($"Poli trovati: {poles.Length} / 2",
            poles.Length == 2 ? EditorStyles.label : EditorStyles.boldLabel);

        if (poles.Length != 2)
            EditorGUILayout.HelpBox("Servono esattamente 2 CorridorPoleScript figli.",
                MessageType.Warning);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Refresh figli"))
            corridor.OnCorridorIdChanged();
    }

    private bool IsNodeTaken(GameObject node, CorridorData self)
    {
        foreach (var c in FindObjectsByType<CorridorData>(FindObjectsSortMode.None))
            if (c != self && c.corridorNode == node) return true;
        return false;
    }
}
#endif