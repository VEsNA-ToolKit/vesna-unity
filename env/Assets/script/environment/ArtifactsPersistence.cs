using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

// ============================================================
// ArtifactsPersistence.cs
// ============================================================
// Scrive su disco un JSON separato con solo gli artefatti
// scoperti durante l'esplorazione, con dettaglio completo.
//
// OUTPUT (esempio):
// {
//   "buildingId": "building",
//   "capturedAt": "2025-06-05T10:00:00Z",
//   "artifacts": [
//     {
//       "artifactId": "ArtefattoUfficio5",
//       "artifactType": "RoomObject",
//       "roomId": "Ufficio5",
//       "x": -8.425, "y": 0.5, "z": 12.98,
//       "wsPort": 3010
//     }
//   ]
// }
//
// FILE: {persistentDataPath}/graph_snapshots/{buildingId}_artifacts.json
// ============================================================

public static class ArtifactsPersistence
{
    private const string SNAPSHOTS_FOLDER = "graph_snapshots";
    private const string FILE_SUFFIX      = "_artifacts.json";

    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        Formatting        = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    // ── Path ─────────────────────────────────────────────────

    public static string BaseDirectory =>
        Path.Combine(Application.persistentDataPath, SNAPSHOTS_FOLDER);

    public static string GetFilePath(string buildingId) =>
        Path.Combine(BaseDirectory, SanitizeName(buildingId) + FILE_SUFFIX);

    // ── API pubblica ─────────────────────────────────────────

    /// <summary>
    /// Salva il dettaglio completo di tutti gli artefatti scoperti
    /// in un file JSON separato dal grafo topologico.
    /// Chiamato da ExplorationManager.OnAllFloorsCompleted().
    /// </summary>
    public static bool Save(
        string buildingId,
        Dictionary<string, HashSet<string>> roomArtifacts,
        Dictionary<string, RoomObjectSnapshot> artifactData)
    {
        if (artifactData == null)
        {
            Debug.LogWarning("[ArtifactsPersistence] Dati artefatti null: file non salvato.");
            return false;
        }

        try
        {
            EnsureDirectoryExists();

            // ── Costruisci lista artefatti completa ───────────
            var artifactList = new List<ArtifactEntry>();
            foreach (var kv in artifactData)
            {
                var snap = kv.Value;
                artifactList.Add(new ArtifactEntry
                {
                    artifactId   = snap.artifactId,
                    artifactType = snap.artifactType,
                    roomId       = snap.roomId,
                    x            = snap.x,
                    y            = snap.y,
                    z            = snap.z,
                    wsPort       = snap.wsPort,
                });
            }

            var output = new ArtifactsSnapshot
            {
                buildingId = buildingId,
                capturedAt = System.DateTime.UtcNow.ToString("O"),
                artifacts  = artifactList,
            };

            // ── Scrivi su disco ───────────────────────────────
            string path = GetFilePath(buildingId);
            string json = JsonConvert.SerializeObject(output, SerializerSettings);
            File.WriteAllText(path, json);

            Debug.Log($"[ArtifactsPersistence] Salvati {artifactList.Count} artefatti → {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ArtifactsPersistence] Errore durante il salvataggio: {ex}");
            return false;
        }
    }

    // ── Utility ──────────────────────────────────────────────

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(BaseDirectory))
            Directory.CreateDirectory(BaseDirectory);
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "unnamed";
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    // ── DTO interni ───────────────────────────────────────────

    [System.Serializable]
    private class ArtifactsSnapshot
    {
        public string            buildingId;
        public string            capturedAt;
        public List<ArtifactEntry> artifacts;
    }

    [System.Serializable]
    private class ArtifactEntry
    {
        public string artifactId;
        public string artifactType;
        public string roomId;
        public float  x, y, z;
        public int    wsPort;
    }
}
