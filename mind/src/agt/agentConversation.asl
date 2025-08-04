// Plans to execute a conversation with a friend met in the scene

// the other agent is free to speak with me
+!startConversation(Friend) <-
    .send(Friend, askOne, talking_to(Agent), Reply);
    !evaluateIfCanStartConversation(Friend, Reply).

// My friend is already talking with another agent
+!evaluateIfCanStartConversation(Friend, Reply) : Reply \== false <-
    .print([Friend, " is already talking with an agent"]);
    !start_walking.

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

@actual_plan_for_conversation // Actual plan for the conversation
+!friend_message(Content)[source(Sender)] : Content \== "Bye!!" <-
    !writeLog(["Message received from ", Sender, ": ", Content]);
    processConversation(Content, Reply);
    !writeLog(["Send to ", Sender, " The content: ", Reply]);
    !update_balloon_message(Reply);
    .wait(2000);
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
