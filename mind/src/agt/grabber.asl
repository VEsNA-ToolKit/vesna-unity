/* INCLUDE */
{ include("libraryPlans.asl") }
{ include("buyItemPlans.asl") }
{ include("initialGoalsPlan.asl") }
{ include("artifacts.asl") }

/* ----------------- INITIAL BELIEFS ----------------- */

/* ----------------- LOGIC PLANS ---------------------*/

@grab_artifact
+!grab_artifact : true <-
    .print("retrieving artifacts...");
    lookupArtifact("envManager", ArtId);
    focus(ArtId);
    !retrieve_nearest_artifacts_by_type("Grabbable", Artifacts);
    Artifacts = [First | _];
    stopFocus(ArtId);
    !reach_destination(First);
    .wait({ +reached(place, Dest) });
    .nth(0, First, Upper); // Get the first character of the string
    .lower_case(Upper, Lower); // Make it lowercase
    .replace(First, Upper, Lower, NormalizedName); // replace the first character.
    lookupArtifact(NormalizedName, GrabArtId);
    focus(GrabArtId);
    attemptGrab[artifact_id(GrabArtId)];
    stopFocus(GrabArtId).

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
    !start_walking.
    
{ include("$jacamo/templates/common-cartago.asl") }
{ include("$jacamo/templates/common-moise.asl") }
{ include("$moise/asl/org-obedient.asl") }
