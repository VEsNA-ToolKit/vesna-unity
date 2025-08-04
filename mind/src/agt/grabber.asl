/* INCLUDE */
{ include("libraryPlans.asl") }
{ include("buyItemPlans.asl") }
{ include("initialGoalsPlan.asl") }
{ include("artifacts.asl")}

/* ----------------- INITIAL BELIEFS ----------------- */

/* ----------------- LOGIC PLANS ---------------------*/
@grab_artifact_wait_discovering_how_to_do
+!grab_artifact : not seen_artifact(_, grabbable) <-
    .print("Agent doesn't know what to grab yet");
    .wait("+seen_artifact(_, grabbable)");
    !grab_artifact.
    
@grab_artifact
+!grab_artifact[source(Sender)] : seen_artifact(ArtifactName, grabbable) <-
    .print("Agent wants to grab the object ", ArtifactName);
    // The following three lines are useful to normalize names such as "EnvManager" to "envManager" so that the lookup works correctly!
    .nth(0, ArtifactName, Upper); // Get the first character of the string
    .lower_case(Upper, Lower); // Make it lowercase
    .replace(ArtifactName, Upper, Lower, NormalizedName); // replace the first character.
    lookupArtifact(NormalizedName, ArtId);
    focus(ArtId);
    // We should wait for the agent to be near the object, before trying to grab the object
    .print("Attempting to grab ", ArtifactName);
    attemptGrab[artifact_id(ArtId)];
    stopFocus(ArtId).

+grabbed(ArtifactName) : true <-
    vesna.grab(ArtifactName).

{ include("$jacamo/templates/common-cartago.asl") }
{ include("$jacamo/templates/common-moise.asl") }
{ include("$moise/asl/org-obedient.asl") }
