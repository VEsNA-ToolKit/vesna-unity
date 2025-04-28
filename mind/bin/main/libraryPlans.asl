{ include("agentConversation.asl") }
{ include("conversation.asl") }

!start_walking.

/* FIRST PLAN TO INITIALIZE THE AGENT */
// // @initialize_agent[atomic]
// // +!initializeAgent(AgentName, AgentClass, AgentPort, initGoals(Goals)) <-
// //         // Establish websocket connection for agent
// //         makeArtifact(AgentName, AgentClass, [AgentName, AgentPort], AgentArtifactId);
// //         focus(AgentArtifactId);
// //         // Wait for signal connection established and then execute goals
// //         .wait("+connection_established(true)");
// //         +connection_established(true);
// //         MsgList = ["Agent - started - Port: ", AgentPort];
// //         -+actual_intention(start_walking);
// //         !start_walking;
// //         !writeLog(MsgList);
// //         // Process all goals
// //         !!process_goals(Goals).

/* PLANS TO EXECUTE AND PROCESS AGENT GOALS */
// // @process_goals
// // +!process_goals([]) <-
// //     MsgList = ["All initial goals have been processed."];
// //     !writeLog(MsgList).
// // 
// // @process_goals_recursive
// // +!process_goals([FirstItem | Rest]) <-
// //     !FirstItem;
// //     !process_goals(Rest). // recursive call

/* REACH DESTINATION PLANS */
@reach_dest_wait_previous_dest
+!reach_dest(DestToReach) : movement_in_progress(ActualDest) <-
    !writeLog(["Agent wants to move to ", DestToReach, " but is already moving towards another destination ", ActualDest]);
    .wait("-movement_in_progress(Shop)");
    !reach_dest(DestToReach).

+!reach_dest(DestToReach) : not movement_in_progress(_) <-
    +movement_in_progress(DestToReach);
    !writeLog(["Go to the ", DestToReach]);
    // // !define_payload("reachDestination", DestToReach, ReturnMsg);
    // // !sendMessageToUnity(ReturnMsg);
    vesna.walk( DestToReach );
    .wait("+reached_destination(DestToReach)");
    !writeLog(["Destination reached ", DestToReach]).

/* STRATEGIES TO RETRIEVE ARTIFACTS BY CALLING ENVIRONMENT MANAGER */
@retrieve_artifacts_by_type[atomic]
+!retrieve_artifacts_by_type(ArtifactType, Artifacts) <-
    lookupArtifact("envManager", LeadArtifactId);
    .my_name(Name);
    retrieveArtifactsByType(ArtifactType, Name)[artifact_id(LeadArtifactId)];
    .wait("+artifact_names(Artifacts)").

@retrieve_nearest_artifacts_by_type[atomic]
+!retrieve_nearest_artifacts_by_type(ArtifactType, Artifacts) <-
    lookupArtifact("envManager", LeadArtifactId);
    .my_name(Name);
    retrieveNearestArtifactByType(ArtifactType, Name)[artifact_id(LeadArtifactId)];
    .wait("+artifact_names(Artifacts)").

@retrieve_all_artifacts[atomic]
+!retrieve_all_artifacts(ArtifactType, Artifacts) <-
    lookupArtifact("envManager", LeadArtifactId);
    .my_name(Name);
    retrieveAllArtifacts(Name)[artifact_id(LeadArtifactId)];
    .wait("+artifact_names(Artifacts)").

/* LOGGING PLAN */
// +!writeLog(MessageList) <-
//     .my_name(Name);
//     .concat(["ASL file: ", Name, " - "], MessageList, FinalMsgList);
//     writeLogForAgent(FinalMsgList).
+!writeLog( MessageList ) <- true.

/* PREPARE AND SEND MESSAGE TO UNITY */
@define_payload // e.g. message(agentName, messageType, messagePayload)
+!define_payload(MessageType, MessagePayload, ReturnMsg) <-
    .my_name(Name);
    ReturnMsg = message(Name, MessageType, MessagePayload).

@send_message_to_unity // forward message to unity side
+!sendMessageToUnity(Map) <-
    !retrieve_agent_artifact_id(AgentArtifactId);
    focus(AgentArtifactId);
    notifyUnity(Map).

/* RETRIEVE AGENT ARTIFACT ID */
@retrieve_agent_artifact_id
+!retrieve_agent_artifact_id(AgentArtifactId) <-
    .my_name(Name);
    .concat(Name, "Agent", AgentArtifact);
    // Lookup and focus on the ReceiverArtifact
    lookupArtifact(AgentArtifact, AgentArtifactId).

/* PLANS FOR AGENT VISION */
// Events that the agent has seen either an artifact or an agent
seen_agent([]).

// Save the seen artifact e.g. seen_artifact(barParadise, bar)
@artifact_seen_append
+artifactSeen(ArtifactElement) : ArtifactElement = artifactinfo(ArtifactName,ArtifactType) <-
    !writeLog(["The avatar has seen the artifact ", ArtifactElement]);
    +seen_artifact(ArtifactName, ArtifactType).

@agent_seen_append
+agentSeen(AgentElement) : seen_agent(SeenElements) <-
    !writeLog(["The avatar has seen another agent ", AgentElement]);
    updateSeenElements(SeenElements, AgentElement, UpdatedSeenElements);
    UpdatedSeenElements = updated_seen_elements(UpdatedElements);
    -+seen_agent(UpdatedElements);
    +new_agent_seen(AgentElement).

/* START WALKING PLANS */
@start_walking
+!start_walking : not busy <-
    // // !define_payload("startWalking", "", ReturnMsg);
    // // !sendMessageToUnity(ReturnMsg).
    vesna.walk( random ).

+!start_walking : busy <-
    !writeLog(["Agent is doing something else"]).

/* CHECK IF FRIEND IS REACHED */
@reached_friend
+reached_friend(Friend) <-
    !writeLog(["I've reached my friend ", Friend]);
    +friend_reached(Friend).

/* UPDATE BALLOON MESSAGE */
@update_balloon_message
+!update_balloon_message(Content) <-
    // // !define_payload("conversation", Content, ReturnMsg);
    // // !sendMessageToUnity(ReturnMsg).
    vesna.says( Content ).