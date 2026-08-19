using System;
using Newtonsoft.Json;
using script.core.model.io;
using script.core.util;
using UnityEngine;
using WebSocketSharp;

// ============================================================
// VesnaAvatarBase.cs  ->  : AgentAvatarSocial
// ============================================================
// Base comune ai due agenti che navigano il grafo:
//   - ExplorerAvatar   (esplora E naviga)
//   - NavigatorAvatar  (solo naviga)
//
// Intercetta SOLO i comandi walk verso un nodo del grafo e li
// gira al MovementModel topologico. Tutto il resto (say, grab,
// stop, rotate, friend, random walk, artefatti) resta gestito
// dalla libreria della prof tramite base.OnMessage.
//
// NON modifica nessuna classe della libreria.
// ============================================================

public abstract class VesnaAvatarBase : AgentAvatarSocial
{
    protected TopologicalMovement topoMover;

    protected override void Awake()
    {
        base.Awake();
        // movementModel è già stato risolto da AgentAvatarSocial.Awake()
        topoMover = movementModel as TopologicalMovement;
        if (topoMover == null)
            Debug.LogWarning($"[{name}] Nessun TopologicalMovement trovato: " +
                             "la navigazione nodo-per-nodo non funzionerà.");

        // L'avatar possiede SEMPRE il server del body (un solo server per body,
        // sulla porta dell'avatar = quella del .jcm dell'agente). Il guard
        // IsServerRunning lo rende idempotente. Il BeliefTransmitter NON apre
        // più niente: trasmette il grafo su questo stesso canale.
        _ = startServer();
    }

    protected override void OnMessage(object sender, MessageEventArgs e)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<WsMessage>(e.Data);
            if (message != null && message.Type == MessageTypes.Walk)
            {
                var innerType = message.Data?["type"]?.ToString();
                if (innerType == "goto_pos" && topoMover != null)
                {
                    var d = message.Data;
                    float px = (float)d["x"];
                    float py = (float)d["y"];
                    float pz = (float)d["z"];
                    string label = (string)d["target"];
                    UnityMainThreadDispatcher.Instance().Enqueue(() => topoMover.GoToPosition(px, py, pz, label));
                    return;
                }
                var walkData = message.Data.ToObject<WalkData>();
                 Debug.Log($"[VesnaAvatarBase] Walk verso '{walkData?.Target}', topoMover null: {topoMover == null}, HasNode: {topoMover?.HasNode(walkData?.Target)}");
                if (walkData != null
                    && !string.IsNullOrEmpty(walkData.Target)
                    && walkData.Target != "random"
                    && topoMover != null
                    && topoMover.HasNode(walkData.Target))
                {
                    string node = walkData.Target;
                    UnityMainThreadDispatcher.Instance().Enqueue(() => topoMover.GoToNode(node));
                    return; // gestito qui: non passa alla libreria
                }
            }
        }
        catch (Exception ex)
        {
           Debug.LogWarning($"[VesnaAvatarBase] Eccezione nel parsing custom: {ex.Message} | topoMover null: {topoMover == null}");
        }

        // Tutti gli altri messaggi: li gestisce la prof.
        base.OnMessage(sender, e);
    }

}