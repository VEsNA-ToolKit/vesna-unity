// ============================================================
// explorer.asl — Variante 2 (invio del grafo completo, in blocco)
// ============================================================

// Piani di movimento della libreria (reach_dest -> walk(goto,Next) a Unity)
{ include("libraryPlans.asl") }
{ include("initialGoalsPlan.asl") }

// L'explorer non vaga: sopprime l'eventuale start_walking della libreria
+!start_walking <- true.

!start.

+!start <-
    !ensure_env_manager;
    .print("[explorer] pronto, ricevo il grafo da Unity...").

// ── Aggancio manuale all'envManager: lookup + focus esplicito ────────────
+!ensure_env_manager <-
    joinWorkspace("/main/main", _);
    lookupArtifact("envManager", EnvId);
    focus(EnvId);
    +env_manager(EnvId);
    .print("[explorer] envManager agganciato: ", EnvId).

-!ensure_env_manager <-
    .print("[explorer] ensure_env_manager fallito, ritento...");
    .wait(500);
    !ensure_env_manager.

// ── Helper: attende env_manager se non ancora disponibile ────────────────
+!get_env(E) : env_manager(E).
+!get_env(E) <-
    .wait({+env_manager(_)}, 10000, _);
    ?env_manager(E).

// ── Ricezione grafo da Unity ──────────────────────────────────────────────
// NB: qui NON si invia più nulla all'envManager pezzo per pezzo.
// I dati restano nella belief base dell'agente (node/9, edge/11,
// connector_link/10, door_dist/4, new_object/7) e vengono spediti
// tutti insieme quando arriva exploration_complete.

+node(Id, Type, Floor, X, Y, Z, CorridorId, PoleLabel, WsPort) <-
    .print("[explorer] nodo: ", Id).

+edge(GoName, From, To, Floor, EdgeType, DoorState, Side, Dir, DistA, DistB, WsPort, IsPhysical) <-
    .print("[explorer] arco: ", GoName, " da ", From, " a ", To).

+connector_link(Id, FloorA, FloorB, Bidirectional, PosAx, PosAy, PosAz, PosBx, PosBy, PosBz) <-
    .print("[explorer] connettore: ", Id, " piano ", FloorA, " <-> ", FloorB).

+door_dist(RoomA, RoomB, Floor, Dist) <-
    true.

+corridor(CorridorId, PoleA, PoleB, Floor, WsPort) <-
    .print("[explorer] corridoio: ", CorridorId);
    ?node(PoleA, _, _, X1, Y1, Z1, _, _, _);
    ?node(PoleB, _, _, X2, Y2, Z2, _, _, _);
    makeArtifact(CorridorId, "artifact.CorridorArtifact",
                 [CorridorId, WsPort, X1, Y1, Z1, X2, Y2, Z2], _).

+new_object(ArtifactId, RoomId, ArtifactType, WsPort, X, Y, Z) <-
    .print("[explorer] oggetto: ", ArtifactId);
    makeArtifact(ArtifactId, "artifact.RoomObjectArtifact",
                 [ArtifactId, WsPort, RoomId, X, Y, Z], Handle);
    focus(Handle).

// ── Grafo completo: raccolta in blocco e invio unico all'envManager ──────
+exploration_complete(BuildingId, _, _, TotalNodes, TotalEdges, _) <-
    .print("[explorer] grafo completo: ", TotalNodes, " nodi, ", TotalEdges, " archi, invio in blocco...");
    !get_env(E);

    // -- Nodi "normali" (stanze, pali corridoio, ecc.) --
    .findall(Id,    node(Id,_,_,_,_,_,_,_,_),    NIds);
    .findall(Type,  node(_,Type,_,_,_,_,_,_,_),  NTypes);
    .findall(Floor, node(_,_,Floor,_,_,_,_,_,_), NFloors);
    .findall(X,     node(_,_,_,X,_,_,_,_,_),     NXs);
    .findall(Y,     node(_,_,_,_,Y,_,_,_,_),     NYs);
    .findall(Z,     node(_,_,_,_,_,Z,_,_,_),     NZs);

    // -- Nodi "connettore" (scale/ascensori tra piani) --
    .findall(Id2,          connector_link(Id2,_,_,_,_,_,_,_,_,_),         CIds);
    .findall("Connector",  connector_link(_,_,_,_,_,_,_,_,_,_),           CTypes);
    .findall(FloorA,       connector_link(_,FloorA,_,_,_,_,_,_,_,_),      CFloors);
    .findall(PosAx,        connector_link(_,_,_,_,PosAx,_,_,_,_,_),       CXs);
    .findall(PosAy,        connector_link(_,_,_,_,_,PosAy,_,_,_,_),       CYs);
    .findall(PosAz,        connector_link(_,_,_,_,_,_,PosAz,_,_,_),       CZs);

    .concat(NIds,    CIds,    AllIds);
    .concat(NTypes,  CTypes,  AllTypes);
    .concat(NFloors, CFloors, AllFloors);
    .concat(NXs,     CXs,     AllXs);
    .concat(NYs,     CYs,     AllYs);
    .concat(NZs,     CZs,     AllZs);

    // -- Archi --
    .findall(From,     edge(_,From,_,_,_,_,_,_,_,_,_,_),     EFroms);
    .findall(To,        edge(_,_,To,_,_,_,_,_,_,_,_,_),       ETos);
    .findall(EdgeType,  edge(_,_,_,_,EdgeType,_,_,_,_,_,_,_), ETypes);
    .findall(Dir,       edge(_,_,_,_,_,_,_,Dir,_,_,_,_),      EDirs);
    .findall(DistA,     edge(_,_,_,_,_,_,_,_,DistA,_,_,_),    EDistAs);
    .findall(DistB,     edge(_,_,_,_,_,_,_,_,_,DistB,_,_),    EDistBs);

    // -- Distanze porte --
    .findall(RoomA, door_dist(RoomA,_,_,_), DARooms);
    .findall(RoomB, door_dist(_,RoomB,_,_), DBRooms);
    .findall(Dist,  door_dist(_,_,_,Dist),  DDists);

    // -- Oggetti --
    .findall(ArtId,  new_object(ArtId,_,_,_,_,_,_),  OArtIds);
    .findall(RoomId, new_object(_,RoomId,_,_,_,_,_), ORoomIds);

    // Gli artefatti porta/varco devono esistere prima del segnale graphReady.
    !create_door_artifacts;

    // -- Invio atomico del grafo completo: una sola operazione CArtAgO --
    setCompleteGraph(
        AllIds, AllTypes, AllFloors, AllXs, AllYs, AllZs,
        EFroms, ETos, ETypes, EDirs, EDistAs, EDistBs,
        DARooms, DBRooms, DDists,
        OArtIds, ORoomIds
    )[artifact_id(E)];

    .print("[explorer] grafo inviato correttamente all'envManager");
    !poll_target.


+current_room(Where) : at(Where) <- true.           // già nota → ignora (anti-stale)
+current_room(Where) <-
	 .abolish(at(_));
    +at(Where);
    .print("[explorer] posizione iniziale: ", Where).


// ── Polling del target dal bridge (ai_bridge.py → target.json) ────────────
+!poll_target <-
    .wait(1500);
    !try_navigate;
    !poll_target.

+!try_navigate : at(Start) & not navigating <-
    vesna.CheckTarget(Goal, Artifact);
    +navigating;
    .print("[explorer] target: ", Goal);
    !navigate_to(Start, Goal);
    !final_approach(Goal, Artifact);
    -navigating.

// rete di sicurezza: guardia falsa (non in at/_ o già navigating) → salta il giro
+!try_navigate <- true.

// CheckTarget ritorna false quando non c'è un nuovo target: normale, pulisci e continua
-!try_navigate <- .abolish(navigating).

// target-stanza puro (ai_bridge.py): Artifact = "" → ci si ferma alla stanza,
// at(_) lo aggiorna già Unity con current_room.
+!final_approach(_, "") <- true.

+!final_approach(Room, Artifact) <-
    !!reach_dest(Artifact);
    -movement_in_progress(Artifact);
    .abolish(at(_));
    +at(Room);
    .print("[explorer] arrivato a ", Artifact, ", stanza corrente: ", Room).


// ── Crea artefatti porte/varchi ───────────────────────────────────────────
// La natura dell'apertura (porta fisica vs varco aperto) è decisa da
// IsPhysical, che arriva direttamente da DoorVarcoScript.elementType
// impostato in Inspector — non dal nome del GameObject, per evitare che
// una convenzione di naming rotta o incoerente generi l'artefatto sbagliato.
// EdgeType == "Central" è escluso perché è l'arco interno del corridoio,
// non un elemento fisico attraversabile (non ha un DoorVarcoScript associato).
// Lo stato Open/Closed modifica solo lo stato iniziale della porta.
+!create_door_artifacts <-
    for( edge(GoName, _, _, _, EdgeType, DoorState, _, Dir, _, _, WsPort, IsPhysical) ) {
        if (not created_opening(GoName) & EdgeType \== "Central") {
            if (IsPhysical) {
                if (DoorState == "Open") { IsOpen = "true" } else { IsOpen = "false" };
                makeArtifact(GoName, "artifact.DoorArtifact",
                             [GoName, WsPort, IsOpen, "false", 0.0, 0.0, 0.0], _);
                +created_opening(GoName)
            } else {
                makeArtifact(GoName, "artifact.VarcoArtifact",
                             [GoName, WsPort, 0.0, 0.0, 0.0], _);
                +created_opening(GoName)
            }
        }
    }.

// ── Naviga verso una stanza usando A* nella mente ────────────────────────
+!navigate_to(StartId, GoalId) <-
    .print("[explorer] Calcolo percorso: ", StartId, " → ", GoalId);
    vesna.ComputePath(StartId, GoalId, Path, Cost);
    if (Path == []) {
        .print("[explorer] Nessun percorso trovato da ", StartId, " a ", GoalId)
    } else {
        .print("[explorer] Percorso (", Cost, "m): ", Path);
        Path = [_First | StepsToWalk];   // scarta il nodo di partenza (ci sono già)
        !follow_path(StepsToWalk);
        .print("[explorer] Percorso completato.")
    }.


// nodo intermedio: c'è ancora del percorso dopo
+!follow_path([Next | Rest]) : Rest \== [] <-
    .print("[explorer] -> ", Next);
    !!reach_dest(Next);
    .wait({ +reached(place, Next) });
    -movement_in_progress(Next);
    .abolish(at(_)); +at(Next);
    .print("[explorer] arrivato in: ", Next);
    !follow_path(Rest).

// ultimo nodo: dopo non c'è più nulla → è la destinazione
+!follow_path([Last]) <-
    .print("[explorer] -> ", Last);
    !!reach_dest(Last);
    .wait({ +reached(place, Last) });
    -movement_in_progress(Last);
    .abolish(at(_)); +at(Last);
    .print("[explorer] sei a destinazione: ", Last).

+!follow_path([]).
