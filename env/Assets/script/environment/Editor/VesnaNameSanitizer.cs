#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class VesnaNameSanitizer
{
    [MenuItem("VESNA/Sanitizza nomi porte")]
    static void Sanitize()
    {
        int n = 0;
        foreach (var d in Object.FindObjectsOfType<DoorVarcoScript>(true))
        {
            string clean = d.gameObject.name.Replace(' ', '_').Replace('-', '_');
            if (clean != d.gameObject.name)
            {
                Undo.RecordObject(d.gameObject, "Sanitize door name");
                d.gameObject.name = clean;
                n++;
            }
        }
        Debug.Log($"[Sanitizer] Rinominate {n} porte/varchi.");
    }

    [MenuItem("VESNA/Sanitizza nomi oggetti")]
    static void SanitizeObjects()
    {
        int n = 0;
        foreach (var a in Object.FindObjectsOfType<Artifact>(true))
        {
            string clean = Clean(a.gameObject.name);
            if (clean != a.gameObject.name)
            {
                Undo.RecordObject(a.gameObject, "Sanitize object name");
                a.gameObject.name = clean;
                n++;
            }
        }
        Debug.Log($"[Sanitizer] Rinominati {n} oggetti/artefatti.");
    }

    static string Clean(string name) => name.Replace(' ', '_').Replace('-', '_');
}
#endif