
//TENTATIVO IN CORSO
count(0).
//count2(0, 0).
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
    .print("Friend", Friend);
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
    vesna.says(Friend, "Heiiii!", "felice");
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
+!join_conversation(NewFriend) : count2(ID, N) & N < 3 <-
    .print("CONTEGGIO AGENTI: ", N, " ID: ", ID);
    !writeLog(["Allowing ", NewFriend, " to join the conversation"]);
    .print("JOIN");
    .my_name(Me);
    ?conversation_for_agent(Me, ID);
    ?conversation(ID, AgentsList);
    .union(AgentsList, [NewFriend], NewAgentsList);
    !increment_counter2(ID, N2);
    -conversation(ID, AgentsList);
    +conversation(ID, NewAgentsList);
    .print("New list: ", NewAgentsList);
    +talking_to(NewFriend);
    !send_to_all(NewAgentsList, tell, conversation_update(ID, NewAgentsList));
    .send(NewFriend, tell, joined_conversation).

+!join_conversation(NewFriend) : count2(ID, N) & N >= 3 <-
    .print("CONTEGGIO AGENTI: ", N, " ID: ", ID);
    .print(["Conversation is full, cannot add ", NewFriend]);
    .send(NewFriend, achieve, walk_and_not_talk).

+!join_conversation(NewFriend) : not count2(ID, _) <- 
    .print("No count2 found, cannot join, retry in another time");
    .wait(1000); 
    -+actual_intention(start_walking);
    !start_walking.

+!walk_and_not_talk[source(NewFriend)] <-
    .print("CAMMINA");
    -+actual_intention(start_walking);
    !start_walking.

// --- Aggiorna la conversazione quando ricevo un update ---
+conversation_update(ID, NewAgentsList)[source(Sender)] : conversation(ID, OldList) <- 
    -conversation(ID, OldList);
    +conversation(ID, NewAgentsList);
    .print(["Conversation ", ID, " aggiornata: ", NewAgentsList]).

+conversation_update(ID, NewAgentsList)[source(Sender)] : not conversation(ID, _) <- 
    +conversation(ID, NewAgentsList);
    .print(["Conversation ", ID, " aggiunta: ", NewAgentsList]).

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

+!finish_conversation[source(Sender)] <- 
    .wait(2000);
    !writeLog(["Conversation finished by ", Sender]);
    .abolish(talking_to(_));
    -+actual_intention(start_walking);
    !start_walking;
    ?conversation(ID, AgentsList);
    .delete(AgentsList, Sender, Others);
    !send_finish_to_all(Others);
    -conversation(ID, AgentsList);
    -count2(ID, _).

+!send_finish_to_all([]) <- .print("Messaggi finish inviati a tutti.").

+!send_finish_to_all([Agent|Rest]) <- 
    .send(Agent, achieve, finish_conversation);
    !send_finish_to_all(Rest).

@finish_other_conversation
+!finish_other_conversation[source(Sender)] <- 
    -talking_to(Sender);
    .wait(2000);
    -+actual_intention(start_walking);
    !start_walking.