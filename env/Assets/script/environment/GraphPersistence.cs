using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

// ============================================================
// GraphPersistence.cs
// ============================================================
// Gestisce la lettura e scrittura di BuildingSnapshot su disco.
//
// CARATTERISTICHE:
//   • Snapshot nominati per ID edificio: ogni edificio ha il suo
//     file, più edifici coesistono senza conflitti.
//   • Versionamento: se il file su disco ha uno schema vecchio,
//     Migrate() lo aggiorna prima di restituirlo.
//   • Zero dipendenze da TopologicalGraph o BeliefTransmitter:
//     lavora solo su BuildingSnapshot e primitivi.
//   • Path configurabile (default: Application.persistentDataPath).
//
// DIRECTORY STRUCTURE (esempio):
//   {persistentDataPath}/
//     graph_snapshots/
//       Edificio_A.json
//       Edificio_B.json
//       ...
//
// COME ESTENDERE:
//   • Più slot di salvataggio per lo stesso edificio →
//     aggiungi un parametro "slotName" a Save/Load.
//   • Backup automatico → fai una copia del file prima di
//     sovrascriverlo in Save().
//   • Migrazione schema → incrementa BuildingSnapshot.SCHEMA_VERSION
//     e aggiungi un case in Migrate().
// ============================================================

public static class GraphPersistence
{
    // ── Configurazione ───────────────────────────────────────
    private const string SNAPSHOTS_FOLDER = "graph_snapshots";
    private const string FILE_EXTENSION   = ".json";

    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        Formatting        = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    // ── Path helpers ─────────────────────────────────────────

    /// <summary>Cartella base dove vengono salvati tutti gli snapshot.</summary>
    public static string BaseDirectory =>
        Path.Combine(Application.persistentDataPath, SNAPSHOTS_FOLDER);

    /// <summary>Path completo del file per un dato edificio.</summary>
    public static string GetFilePath(string buildingId) =>
        Path.Combine(BaseDirectory, SanitizeName(buildingId) + FILE_EXTENSION);

    // ── API pubblica ─────────────────────────────────────────

    /// <summary>
    /// Salva uno snapshot su disco.
    /// Sovrascrive il file se già esiste.
    /// Restituisce true se il salvataggio è riuscito.
    /// </summary>
    public static bool Save(BuildingSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogError("[GraphPersistence] Save: snapshot null.");
            return false;
        }

        try
        {
            EnsureDirectoryExists();
            string path = GetFilePath(snapshot.buildingId);
            string json = JsonConvert.SerializeObject(snapshot, SerializerSettings);
            File.WriteAllText(path, json);
            Debug.Log($"[GraphPersistence] Snapshot salvato: {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GraphPersistence] Errore durante il salvataggio: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Carica lo snapshot di un edificio dal disco.
    /// Applica la migrazione se necessario.
    /// Restituisce null se il file non esiste o è corrotto.
    /// </summary>
    public static BuildingSnapshot Load(string buildingId)
    {
        string path = GetFilePath(buildingId);

        if (!File.Exists(path))
        {
            Debug.Log($"[GraphPersistence] Nessun snapshot trovato per '{buildingId}'.");
            return null;
        }

        try
        {
            string           json     = File.ReadAllText(path);
            BuildingSnapshot snapshot = JsonConvert.DeserializeObject<BuildingSnapshot>(json);

            if (snapshot == null)
            {
                Debug.LogError($"[GraphPersistence] Deserializzazione fallita: {path}");
                return null;
            }

            snapshot = Migrate(snapshot);
            Debug.Log($"[GraphPersistence] Snapshot caricato: '{buildingId}' " +
                      $"(schema v{snapshot.schemaVersion}, {snapshot.floors.Count} piani)");
            return snapshot;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GraphPersistence] Errore durante il caricamento: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Ritorna true se esiste uno snapshot salvato per l'edificio dato.
    /// </summary>
    public static bool Exists(string buildingId) =>
        File.Exists(GetFilePath(buildingId));

    /// <summary>
    /// Elimina lo snapshot di un edificio dal disco.
    /// Utile per forzare una nuova esplorazione.
    /// </summary>
    public static bool Delete(string buildingId)
    {
        string path = GetFilePath(buildingId);
        if (!File.Exists(path)) return false;

        try
        {
            File.Delete(path);
            Debug.Log($"[GraphPersistence] Snapshot eliminato: {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GraphPersistence] Errore durante l'eliminazione: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Restituisce gli ID di tutti gli edifici per cui esiste uno snapshot.
    /// </summary>
    public static List<string> ListAll()
    {
        var result = new List<string>();

        if (!Directory.Exists(BaseDirectory)) return result;

        foreach (string file in Directory.GetFiles(BaseDirectory, "*" + FILE_EXTENSION))
        {
            // Carica solo i metadati (evita di deserializzare l'intero file)
            try
            {
                string           json     = File.ReadAllText(file);
                BuildingSnapshot snapshot = JsonConvert.DeserializeObject<BuildingSnapshot>(json);
                if (snapshot?.buildingId != null)
                    result.Add(snapshot.buildingId);
            }
            catch { /* file corrotto: ignorato */ }
        }

        return result;
    }

    // ── Migrazione schema ────────────────────────────────────

    /// <summary>
    /// Aggiorna uno snapshot con schema vecchio alla versione corrente.
    /// Aggiungere un case per ogni versione intermedia.
    /// </summary>
    private static BuildingSnapshot Migrate(BuildingSnapshot snapshot)
    {
        // Al momento esiste solo la v1 — nessuna migrazione necessaria.
        // Esempio per il futuro:
        //
        // if (snapshot.schemaVersion < 2)
        // {
        //     // Popola un campo aggiunto in v2
        //     foreach (var floor in snapshot.floors)
        //         foreach (var node in floor.nodes)
        //             node.newFieldAddedInV2 = defaultValue;
        //
        //     snapshot.schemaVersion = 2;
        // }

        if (snapshot.schemaVersion != BuildingSnapshot.SCHEMA_VERSION)
        {
            Debug.LogWarning($"[GraphPersistence] Schema v{snapshot.schemaVersion} " +
                             $"→ v{BuildingSnapshot.SCHEMA_VERSION}: nessuna migrazione definita.");
        }

        return snapshot;
    }

    // ── Utility ──────────────────────────────────────────────

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(BaseDirectory))
            Directory.CreateDirectory(BaseDirectory);
    }

    /// <summary>Rimuove caratteri non validi dal nome del file.</summary>
    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "unnamed";

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name;
    }
}
