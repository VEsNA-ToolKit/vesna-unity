
count(0).
conversation_lock(false).

// ---- helper rules ----
in_conversation(Agent, Conversation) :- conversation_members(Conversation, Members) & member(Agent, Members).

+!increment_counter(N1) : count(N) <- 
    N1 = N + 1;
    -count(N);
    +count(N1);
    .print("Contatore incrementato: ", N1).

+!increment_counter2(ID, N1) : count2(ID, N) <-
    N1 = N + 1;
    -count2(ID, N);
    +count2(ID, N1);
    .print("Contatore conversazione: ", ID ," incrementato, numero agenti: ", N1).

// Provo a iniziare una conversazione con Friend
+!startConversation(Friend)<-
    .send(Friend, tell, conversation_lock(Friend, true));
    .send(Friend, askOne, talking_to(Any), Reply);
    .print("REPLY: ", Reply);
    .my_name(Me);
    .print("Friend: ", Friend);
    !evaluateIfCanStartConversation(Friend, Me, Reply).


// Friend è già impegnato: provo a unirmi
+!evaluateIfCanStartConversation(Friend, Me, talking_to(OtherAgent)[source(Friend)]) <- 
    .print([Friend, " is already talking with someone else, trying to join"]);
    .my_name(Me);
    .send(Friend, achieve, join_conversation(Me)).

// Friend è libero: inizio la conversazione
+!evaluateIfCanStartConversation(Friend, Me, false) <-
    vesna.stop;
    // Genera nuovo ID incrementando il contatore
    !increment_counter(NewID);
    // Crea la conversation con solo Me dentro
    .my_name(Me);
    +conversation(NewID, [Me, Friend]);
    .print("Lista: ", [Me, Friend]);
    !send_to_all([Me, Friend], tell, conversation_update(NewID, [Me, Friend]));
    +count2(NewID, 2);
    .print(Me);
    .print("FRIEND: ", Friend, " si è unito alla conversazione");
    .print(Friend);

    // Comunica al Friend che ora parlo con Me
    .send(Me, tell, talking_to([Friend]));
    .send(Friend, tell, talking_to([Me]));
    -+actual_intention(talk);
    .print(["Starting conversation with: ", Friend, " in conversation ", NewID]);
    //vesna.says("inform", Friend, "Heiiii!", "felice");
    vesna.says(Friend, "Heiiii!", "happy");
    .send(Friend, achieve, stop_and_talk).   

@agent_not_busy_start_conversation
+!stop_and_talk[source(Friend)] : not busy <- 
    .print("MIKI PROVA");
    -+actual_intention(talk);
    vesna.walk(Friend).
/*
@agent_not_busy_start_conversation
+!stop_and_talk[source(Friend)] : busy <- 
    .print("PROVA2");
    .my_name(Me);
    .send(Me, achieve, join_conversation(Friend)).*/

// Ricevo una richiesta di entrare in una conversazione
// --- Utility: invia un messaggio a tutti gli agenti di una lista ---
+!send_to_all([], _Perf, _Content) <- true.
+!send_to_all([A|Rest], Perf, Content) <- 
    .send(A, Perf, Content);
    !send_to_all(Rest, Perf, Content).

// --- JOIN FIX ---
// --- JOIN CONVERSATION FINO A 3 AGENTI ---
+!join_conversation(NewFriend) : count2(ID, N) & N < 3 <- 
    .print("CONTEGGIO AGENTI: ", N, " ID: ", ID);
    .my_name(Me);
    ?conversation(ID, AgentsList);
    .union(AgentsList, [NewFriend], NewAgentsList);
    -conversation(ID, AgentsList);
    +conversation(ID, NewAgentsList);
    Length = .length(NewAgentsList);
    -count2(ID, _);
    +count2(ID, Length);
    !send_to_all(NewAgentsList, tell, conversation_update(ID, NewAgentsList));
    .send(NewFriend, tell, joined_conversation);
    .print("NewFriend ", NewFriend, " successfully joined conversation ", ID).

// --- SE IL LIMITE È RAGGIUNTO ---
+!join_conversation(NewFriend) : count2(ID, N) & N >= 3 <- 
    .print("Conversation full (", N, " agents), cannot add ", NewFriend);
    .send(NewFriend, achieve, walk_and_not_talk).

// --- RETRY SE count2 NON ESISTE ANCORA ---
+!join_conversation(NewFriend) : not count2(ID, _) <- 
    .print("Conversation data not ready yet for ", NewFriend, ", retrying...");
    .wait(500);       
    !join_conversation(NewFriend);

    .print("NewFriend: ", NewFriend);
    .print("No count2 found, cannot join, retry in another time");
    .wait(1000); 
    -talking_to(_);
    -+actual_intention(start_walking);
    !start_walking.

+!walk_and_not_talk[source(NewFriend)] <-
    .wait(2000);
    .print("CAMMINA");
    -actual_intention(talk);
    -+actual_intention(start_walking);
    !start_walking.

// --- Aggiorna la conversazione quando ricevo un update ---
// Caso in cui ho già la conversazione
+conversation_update(ID, NewAgentsList)[source(Sender)] : conversation(ID, OldList) <- 
    -conversation(ID, OldList);
    +conversation(ID, NewAgentsList);
    Length = .length(NewAgentsList);
    -count2(ID, _);                
    +count2(ID, Length);           
    .print(["Conversation ", ID, " aggiornata: ", NewAgentsList, " ", Length, " agenti"]).

// Caso in cui è la prima volta che sento parlare di questa conversazione
+conversation_update(ID, NewAgentsList)[source(Sender)] : not conversation(ID, _) <- 
    +conversation(ID, NewAgentsList);
    Length = .length(NewAgentsList);
    +count2(ID, Length);
    .print(["Conversation ", ID, " aggiunta: ", NewAgentsList, " ", Length, " agenti"]).

+joined_conversation[source(Friend)] <-
    .print(["Joined conversation with ", Friend]);
    +talking_to(Friend);
    -+actual_intention(talk);
    vesna.walk(Friend);
    .wait(1000);
    vesna.says(Friend, "Ciao, mi unisco anch'io!").

@actual_plan_for_conversation
+!friend_message(Content)[source(Sender)] : Content \== "Bye!!" <- 
    vesna.stop;
    vesna.says(Sender, Content);
    !writeLog(["Message received from ", Sender, ": ", Content]);
    processConversation(Content, Reply);
    !update_balloon_message(Reply);
    .wait(10000);
    .send(Sender, achieve, friend_message(Reply)).

@friend_message_finish_conversation
+!friend_message("Bye!!")[source(Sender)] <-
    !update_balloon_message("Scambio le informazioni");
    .wait(2000);
    .print(Sender);
    ?conversation_for_agent(Sender, ID);
    ?conversation(ID, AgentsList);
    .findall(seen_artifact(A, T), seen_artifact(A, T), Artifacts);
    .send(Sender, tell, Artifacts);
    !send_finish_to_all(AgentsList);
    !update_balloon_message(Artifacts);
    -+actual_intention(start_walking);
    !start_walking.

+!send_finish_to_all([]) <- .print("Messaggi finish inviati a tutti.").

+!send_finish_to_all([Agent|Rest]) <- 
    .send(Agent, achieve, finish_conv);
    !send_finish_to_all(Rest).


// --- CHIUSURA CONVERSAZIONE ---
/*+!friend_message("Bye!!")[source(Sender)] <- 
    !update_balloon_message("Scambio le informazioni");
    .wait(2000);
    .print(">>> Fine conversazione avviata da ", Sender);
    !finish_conversation(Sender).*/

+!finish_conversation(Sender) <-
    
    -conversation(_, _);
    -count2(_, _);
    -conversation_lock(_, _);
    -talking_to(_);
    -friend_reached(_);
    -actual_intention(talk);

    +conversation_lock(false);

    +actual_intention(start_walking);
    .print(">>> RESET completato (finish_conversation da ", Sender, ")");
    !start_walking.

+!finish_conv <- 
    !finish_conversation("self").   // usa la stessa logica anche quando sei tu a chiudere

+!finish_other_conversation[source(Sender)] <- 
    .print(">>> Altro agente ", Sender, " ha chiuso, resetto anch’io");
    !finish_conversation(Sender).