/*
// Plans to execute a conversation with a friend met in the scene

// the other agent is free to speak with me
+!startConversation(Friend) <-
    .send(Friend, askOne, talking_to(Agent), Reply);
    !evaluateIfCanStartConversation(Friend, Reply).

// My friend is already talking with another agent
+!evaluateIfCanStartConversation(Friend, Reply) : Reply \== false <-
    .print([Friend, " is already talking with an agent"]);
    vesna.stop.

// My friend is talking with nobody
+!evaluateIfCanStartConversation(Friend, false) <-
    // Tell the agent that now is going to talk with me
    .send(Friend, tell, talking_to(Me));
    +talking_to(Friend);
    -+actual_intention(talk);
    .print(["Starting conversation with: ", Friend]);
    Content = "Heiiii!";
    .print(["Sending to ", Friend, ": ", Content]);
    // // !define_payload("conversation", Content, ReturnMsg);
    // // !sendMessageToUnity(ReturnMsg);
    vesna.says( Friend, Content );
    // // .my_name(Me);
    .wait(2000);
    .send(Friend, achieve, stop_and_talk).

@agent_not_busy_start_conversation
+!stop_and_talk[source(Friend)] : not busy <-
    !writeLog(["My friend ", Friend, " asked me to talk."]);
    -+actual_intention(talk);
    // //  !define_payload("reachFriend", Friend, ReturnMsg);
    // //  !sendMessageToUnity(ReturnMsg).
    vesna.walk( Friend ).

@agent_busy_into_another_conversation
+!stop_and_talk(Friend) : busy <-
    -talking_to(_);
    .send(Friend, achieve, start_walking).

@actual_plan_for_conversation // Actual plan for the conversation - DA RIVEDERE
+!friend_message(Content)[source(Sender)] : Content \== "Bye!!" <-
    !writeLog(["Message received from ", Sender, ": ", Content]);
    processConversation(Content, Reply);
    !writeLog(["Send to ", Sender, " The content: ", Reply]);
    !update_balloon_message(Reply);
    .wait(10000);
    .send(Sender, achieve, friend_message(Reply)).

@friend_message_finish_conversation
+!friend_message(Content)[source(Sender)] : Content == "Bye!!" <-
    !writeLog(["Message received from ", Sender, ": ", Content]);
    !update_balloon_message("Sending my knowledge");
    .wait(2000);
    // Exchange all artifact information
    .findall(seen_artifact(A, T), seen_artifact(A, T), Artifacts);
    .send(Sender, tell, Artifacts);
    .send(Sender, achieve, finish_conversation);
    !update_balloon_message(Artifacts).

@finished_conversation
+!finish_conversation[source(Sender)] <-
    !update_balloon_message("Sending my knowledge");
    .wait(2000);
    .findall(seen_artifact(A, T), seen_artifact(A, T), Artifacts);
    .send(Sender, tell, Artifacts);
    !writeLog(["Conversation finished"]);
    .wait(2000);
    !update_balloon_message(Artifacts);
    .wait(3000);
    // resume walking
    -+actual_intention(start_walking);
    !start_walking;
    -talking_to(_);
    .send(Sender, achieve, finish_other_conversation);
    -friend_reached(Friend).

@finish_other_conversation
+!finish_other_conversation[source(Sender)] <-
    !writeLog(["Finished conversation"]);
    -talking_to(_)[source(Sender)];
    .wait(2000);
    // Resume previous intention
    -+actual_intention(start_walking);
    !start_walking;
    -friend_reached(Friend).
*/
+!increment_counter(N1) : count(N) <- 
    N1 = N + 1;
    -count(N);
    +count(N1);
    .print("Contatore incrementato: ", N1).

+!send_finish_to_all([]) <- .print("Messaggi finish inviati a tutti.").

+!send_finish_to_all([Agent|Rest]) <-
    .send(Agent, achieve, finish_conversation);
    !send_finish_to_all(Rest).

// Provo a iniziare una conversazione con Friend
+!startConversation(Friend) <-
    .send(Friend, askOne, talking_to(Any), Reply);
    !evaluateIfCanStartConversation(Friend, Reply).

// Friend è già impegnato: provo a unirmi
+!evaluateIfCanStartConversation(Friend, Reply) : Reply \== false <- 
    .print([Friend, " is already talking with someone, trying to join"]);
    .my_name(Me);
    .send(Friend, achieve, join_conversation(Me)).

// Friend è libero: inizio la conversazione
+!evaluateIfCanStartConversation(Friend, false) <-
    // Genera nuovo ID incrementando il contatore
    !increment_counter(NewID);
    // Crea la conversation con solo Me dentro
    .my_name(Me);
    +conversation(NewID, [Me, Friend]);
    .print(Me);
    .print(Friend);

    // Comunica al Friend che ora parlo con Me
    .send(Friend, tell, talking_to(Me));
    +talking_to(Friend);
    -+actual_intention(talk);
    .print(["Starting conversation with: ", Friend, " in conversation ", NewID]);
    vesna.says(Friend, "Heiiii!");
    .wait(2000);
    .send(Friend, achieve, stop_and_talk).


@agent_not_busy_start_conversation
+!stop_and_talk[source(Friend)] : not busy <- 
    !writeLog(["My friend ", Friend, " asked me to talk."]);
    -+actual_intention(talk);
    vesna.walk(Friend).

@agent_busy_into_another_conversation
+!stop_and_talk(Friend) : busy <- 
    .send(Friend, achieve, join_conversation(Me)).

// Ricevo una richiesta di entrare in una conversazione
// --- Utility: invia un messaggio a tutti gli agenti di una lista ---
+!send_to_all([], _Perf, _Content) <- true.
+!send_to_all([A|Rest], Perf, Content) <- 
    .send(A, Perf, Content);
    !send_to_all(Rest, Perf, Content).

// --- JOIN FIX ---
+!join_conversation(NewFriend)[source(NewFriend)] <-
    !writeLog(["Allowing ", NewFriend, " to join the conversation"]);
    .my_name(Me);
    ?conversation_for_agent(Me, ID);
    ?conversation(ID, AgentsList);
    .union(AgentsList, [NewFriend], NewAgentsList);
    -conversation(ID, AgentsList);
    +conversation(ID, NewAgentsList);
    .print("New list: ", NewAgentsList);
    +talking_to(NewFriend);
    !send_to_all(NewAgentsList, tell, conversation_update(ID, NewAgentsList));
    .send(NewFriend, tell, joined_conversation).

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
    vesna.says(Friend, "Ciao, mi unisco anch'io!");
    .print("PROVA").

@actual_plan_for_conversation
+!friend_message(Content)[source(Sender)] : Content \== "Bye!!" <- 
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
    !start_walking.


+!finish_conversation[source(Sender)] <-
    .wait(2000);
    !writeLog(["Conversation finished by ", Sender]);
    // Rimuovo tutte le talking_to locali
    .abolish(talking_to(_));
    // Rimuovo la conversazione
    ?conversation(ID, AgentsList);
    -conversation(ID, AgentsList);
    // Riprendo a camminare
    -+actual_intention(start_walking);
    !start_walking.


@finish_other_conversation
+!finish_other_conversation[source(Sender)] <- 
    -talking_to(Sender);
    .wait(2000);
    -+actual_intention(start_walking);
    !start_walking.




