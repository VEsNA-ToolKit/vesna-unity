// vesna.asl
// Sovrascrive il piano della libreria che fa vesna.walk(random) subito
+!start_walking <- true.

{ include("libraryPlans.asl") }
{ include("initialGoalsPlan.asl") }

// ── Avvio: aggancio envManager + loop comandi ─────────────────────────────
!start.
+!start <-
    .wait(2000);
    .print("[vesna] avviata, aggancio envManager...");
    !ensure_env_manager;
    .print("[vesna] attendo grafo pronto...");
    !wait_graph_ready;
    .print("[vesna] attendo posizione iniziale...");
    .wait({+current_room(_)}, 60000, _);
    ?current_room(Start);
    .print("[vesna] posizione iniziale: ", Start);
    !check_target_loop.

+!wait_graph_ready : graphReady(true).
+!wait_graph_ready <-
    .wait({+graphReady(true)}, 120000, _).

// ── Aggancio manuale all'envManager (stesso pattern dell'explorer) ─────────
+!ensure_env_manager <-
	joinWorkspace("/main/main", _);
    lookupArtifact("envManager", EnvId);
    focus(EnvId);
    +env_manager(EnvId);
    .print("[vesna] envManager agganciato: ", EnvId).

-!ensure_env_manager <-
    .print("[vesna] ensure_env_manager fallito, ritento...");
    .wait(500);
    !ensure_env_manager.

// ── Loop: controlla periodicamente se è arrivato un nuovo target ──────────
+!check_target_loop <-
    .wait(2000);
    if (vesna.CheckTarget(Target, Artifact)) {
        .print("[vesna] nuovo comando ricevuto: ", Target, " (oggetto: ", Artifact, ")");
        !go_to(Target, Artifact);
    };
    !check_target_loop.

+current_room(_) : at(_) <- true.            // già nota → ignora (anti-stale)
+current_room(Where) <-
    +at(Where);
    .print("[vesna] posizione iniziale: ", Where).

// ── Goal principale: vai verso Target ─────────────────────────────────────

// già in movimento: ignora il comando (Correzione 3 della prof)
+!go_to(Target, _) : movement_in_progress(ActualDest) <-
    .print("[vesna] Comando ignorato: movimento in corso verso ", ActualDest).

// comando valido: naviga alla stanza, poi (se artefatto) avvicinati all'oggetto
+!go_to(Target, Artifact) : at(Start) & env_manager(EnvId) <-
    .print("[vesna] da ", Start, " verso ", Target);
    computePath(Start, Target, ResolvedGoal, Path, Cost)[artifact_id(EnvId)];
    if (Path == []) {
        if (not path_attempts(_)) { +path_attempts(0) };
        ?path_attempts(N);
        if (N >= 15) {
            -path_attempts(_);
            .print("[vesna] ERRORE: nessun percorso verso ", Target, " dopo ", N, " tentativi");
        } else {
            M = N + 1; -+path_attempts(M);
            .wait(1000);
            !go_to(Target, Artifact);
        };
    } else {
        -path_attempts(_);                   // reset al successo
        .print("[vesna] percorso verso ", ResolvedGoal, " (", Cost, "m): ", Path);
        Path = [_First|StepsToWalk];
        !follow_path(StepsToWalk);
        .print("[vesna] arrivata alla stanza: ", Target);
        !final_approach(Target, Artifact);
    }.

+!go_to(Target, _) <-
    .print("[vesna] posizione non nota o envManager non pronto, ignoro: ", Target).


// ── Approccio finale all'oggetto (come explorer_v1) ───────────────────────
// target-stanza puro: nessun oggetto → ci si ferma alla stanza
+!final_approach(_, "") <- true.

// target-artefatto: gamba finale verso l'oggetto via il suo ID.
// fire-and-forget: l'arrivo all'ArtifactObject_* torna con receiver:null e viene
// scartato lato VesnaAgent, quindi NON si aspetta +reached(place, Artifact).
+!final_approach(Room, Artifact) <-
    !!reach_dest(Artifact);
    -movement_in_progress(Artifact);
    .abolish(at(_));
    +at(Room);
    .print("[vesna] arrivato a ", Artifact, ", stanza corrente: ", Room).

    
// nodo intermedio: c'è ancora del percorso dopo
+!follow_path([Next | Rest]) : Rest \== [] <-
    .print("[vesna] -> ", Next);
    !!reach_dest(Next);
    .wait({ +reached(place, Next) });
    -movement_in_progress(Next);
    .abolish(at(_)); +at(Next);
    .print("[vesna] arrivato in: ", Next);
    !follow_path(Rest).

// ultimo nodo: dopo non c'è più nulla → è la destinazione
+!follow_path([Last]) <-
    .print("[vesna] -> ", Last);
    !!reach_dest(Last);
    .wait({ +reached(place, Last) });
    -movement_in_progress(Last);
    .abolish(at(_)); +at(Last);
    .print("[vesna] sei a destinazione. Percorso completato").
+!follow_path([]).


// Sovrascrive il piano della libreria che fa vesna.walk(random) subito
+!start_walking <- true.