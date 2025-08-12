/* INCLUDE */
{ include("libraryPlans.asl") }
{ include("buyItemPlans.asl") }
{ include("initialGoalsPlan.asl") }
{ include("artifacts.asl") }

/* ----------------- INITIAL BELIEFS ----------------- */

/* ----------------- LOGIC PLANS ---------------------*/
@grab_artifact_discovering
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
    .print("Attempting to grab ", ArtifactName);
    attemptGrab[artifact_id(ArtId)];
    stopFocus(ArtId).

@release_artifact_discovering
+!release_artifact : not holding(_) <-
    .wait("+holding(_)");
    !release_artifact.

@release_artifact
+!release_artifact[source(Sender)] : holding(ArtifactName) <-
    .print("Agent is releasing the artifact");
    .nth(0, ArtifactName, Upper); // Get the first character of the string
    .lower_case(Upper, Lower); // Make it lowercase
    .replace(ArtifactName, Upper, Lower, NormalizedName); // replace the first character.
    lookupArtifact(NormalizedName, ArtId);
    focus(ArtId);
    attemptRelease[artifact_id(ArtId)].

+released(ArtifactName, Position, Rotation) : true <-
    vesna.release(ArtifactName, Position, Rotation);
    -holding(ArtifactName);
    .print("Agent released the artifact").

+grabbed(ArtifactName) : true <-
    vesna.grab(ArtifactName);
    +holding(ArtifactName);
    .wait(2000);
    !release_artifact.

{ include("$jacamo/templates/common-cartago.asl") }
{ include("$jacamo/templates/common-moise.asl") }
{ include("$moise/asl/org-obedient.asl") }
