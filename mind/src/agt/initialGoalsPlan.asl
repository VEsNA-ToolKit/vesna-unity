/* GOALS AGENTS MIGHT HAVE AT STARTUP */

@go_home
+!go_home : true <-
    -+actual_intention(go_home);
    !writeLog(["Agent wants to go home"]);
    !retrieve_nearest_artifacts_by_type("SkyScraper", Artifacts);
    Artifacts = [First | Rest];
    !reach_destination(First);
    !writeLog(["Waiting for reached_destination for ", First]);
    .wait( { + reached(place, Dest) });
    !writeLog(["Received reached_destination for ", Dest]);
    .wait(1000);
    !start_walking;
    +actual_intention(start_walking);
    -movement_in_progress(_).

// agent doesn't know where the bar is so waits for knowledge
@buy_coffee_wait_discovering_how_to_do
+!buy_coffee : not seen_artifact(_, bar) <-
    .print("Agent doesn't know how to buy coffee yet");
    .wait("+seen_artifact(_, bar)");
    !buy_coffee.

@buy_coffee
+!buy_coffee
    : seen_artifact(ArtifactName, bar) & not talking_to(_) 
    <-  -+actual_intention(buy_coffee);
        +busy;
        !update_balloon_message("Agents knows how to buy coffee");
        .wait(2000);
        !writeLog(["Agent wants to buy a coffee"]);
        !reach_destination(ArtifactName);
        !writeLog(["Waiting for reached_destination for ", ArtifactName]);
        .wait( { +reached( place, Dest) } );
        !writeLog(["Received reached_destination for ", Dest]);
        lookupArtifact(ArtifactName, ArtifactId);
        focus(ArtifactId);
        .wait(1000);
        !joinQueue(ArtifactName, ArtifactId);
        .wait("+can_proceed"); // wait for my turn
        !check_item_and_buy(ArtifactName, espresso, 1); // buy item
        .wait(1000);
        -+actual_intention(start_walking);
        -busy;
        !start_walking;
        -movement_in_progress(_).

@buy_coffee_wait_conversation_finish
+!buy_coffee : seen_artifact(ArtifactName, bar) & talking_to(_) <-
    .wait("-talking_to(_)");
    !buy_coffee.

+!reach_destination(ArtifactName) : movement_in_progress(OldArtifact) <-
    !writeLog(["Already moving to another artifact ", OldArtifact]);
    .wait("-movement_in_progress(OldArtifact)");
    !reach_destination(ArtifactName).

+!reach_destination(ArtifactName) : not movement_in_progress(_) <-
    +movement_in_progress(ArtifactName);
    // Stop the agent from random walk
    // // !define_payload("stopAgent", ArtifactName, ReturnMsg);
    // // !sendMessageToUnity(ReturnMsg);
    vesna.stop;
    .wait(1000);
    // Reach specific destination
    // // !define_payload("reachDestination", ArtifactName, ReturnMsg1);
    // // !sendMessageToUnity(ReturnMsg1).
    vesna.walk( ArtifactName ).

@call
+!call : true <-
    .wait(2000);
    !writeLog(["Calling...."]);
    .wait(2000);
    !writeLog(["Hi mum!"]);
    .wait(3000);
    !writeLog(["Hi dad!"]).
    //!call.

@call_parents
+!call_parents : true <-
    !!call.