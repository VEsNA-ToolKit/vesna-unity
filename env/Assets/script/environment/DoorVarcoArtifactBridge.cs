using UnityEngine;
using Newtonsoft.Json;

// ============================================================
// DoorVarcoArtifactBridge.cs
// ============================================================
// Collega il prefab fisico (DoorVarcoScript) al suo artefatto
// JaCaMo (DoorArtifact o VarcoArtifact) via WebSocket.
//
// PERCHÉ ESISTE SEPARATO DA DoorVarcoScript:
// DoorVarcoScript gestisce la fisica (aprire, chiudere, collider).
// DoorVarcoArtifactBridge gestisce la comunicazione con JaCaMo.
// Separazione netta: fisica vs comunicazione.
//
// COME FUNZIONA:
//   1. All'avvio apre un WebSocketChannel sulla propria porta
//   2. Quando DoorVarcoScript cambia stato fisico,
//      questo bridge notifica JaCaMo via "update_status"
//   3. Quando JaCaMo manda "open_door", questo bridge
//      chiama TryOpen() su DoorVarcoScript
//
// SETUP NELL'INSPECTOR:
//   - artifactName : nome dell'artefatto JaCaMo (es. "door_101")
//                    deve essere uguale al nome nel .jcm
//   - port         : porta WebSocket (diversa per ogni porta)
//   - doorScript   : riferimento al DoorVarcoScript sullo stesso GO
//
// Il nome del GameObject Unity DEVE corrispondere ad artifactName.
// ============================================================

public class DoorVarcoArtifactBridge : AbstractMasElement
{
    [Header("Configurazione JaCaMo")]
    // Nome dell'artefatto nel .jcm — deve matchare esattamente
    public string artifactName => gameObject.name;

    [Header("Riferimento fisico")]
    public DoorVarcoScript doorScript;

    private bool lastIsOpen   = false;
    private bool lastIsLocked = false;

    void Awake()
    {
        if (!Application.IsPlaying(gameObject)) return;
         objInUse = gameObject;
         port = PortAssigner.GetPort(gameObject.name);

        if (doorScript == null)
            doorScript = GetComponent<DoorVarcoScript>();

        // Apre la connessione WebSocket con JaCaMo
        // "port" è la variabile di AbstractMasElement
         port = PortAssigner.GetPort(gameObject.name);
        initializeWebSocketConnection(OnMessageFromJacamo);
        _ = startServer();

        // Snapshot dello stato iniziale
        lastIsOpen   = doorScript != null && doorScript.isOpen;
        lastIsLocked = doorScript != null && doorScript.isLocked;
    }

    // --------------------------------------------------------
    // POLLING: rileva cambi di stato fisico e notifica JaCaMo
    // Unity non ha eventi built-in su cambio proprietà bool,
    // quindi controlliamo ogni frame (leggero, sono solo bool).
    // --------------------------------------------------------
    void Update()
    {
        if (doorScript == null) return;

        bool currentOpen   = doorScript.isOpen;
        bool currentLocked = doorScript.isLocked;

        if (currentOpen != lastIsOpen || currentLocked != lastIsLocked)
        {
            lastIsOpen   = currentOpen;
            lastIsLocked = currentLocked;
            NotifyJacamo();
        }
    }

    private void NotifyJacamo()
    {
        var payload = new {
            operation = "update_status",
            param = new {
                isOpen   = lastIsOpen,
                isLocked = lastIsLocked
            }
        };

        wsChannel?.sendMessage(
            UnityJacamoIntegrationUtil.CreateAndConvertJacamoMessageIntoJsonString(
                "update_status", null, null,
                artifactName,
                payload
            )
        );

        Debug.Log($"[DoorBridge] {artifactName}: isOpen={lastIsOpen} isLocked={lastIsLocked}");
    }

    // --------------------------------------------------------
    // RICEZIONE DA JACAMO
    // Gestisce i comandi che JaCaMo manda alla porta fisica.
    // Tutto sul main thread via UnityMainThreadDispatcher.
    // --------------------------------------------------------
    private void OnMessageFromJacamo(object sender,
                                      WebSocketSharp.MessageEventArgs e)
    {
        try
        {
            var msg = JsonConvert.DeserializeObject<ArtifactMessage>(e.Data);
            if (msg == null) return;

            switch (msg.MessagePayload)
            {
                case "open_door":
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        doorScript?.TryOpen();
                        Debug.Log($"[DoorBridge] {artifactName}: open_door ricevuto");
                    });
                    break;

                case "close_door":
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        doorScript?.Close();
                        Debug.Log($"[DoorBridge] {artifactName}: close_door ricevuto");
                    });
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DoorBridge] {artifactName} OnMessage error: " + ex);
        }
    }
}