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
    !retrieve_nearest_artifacts_by_type("Cylinder", Artifacts);
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
    stopFocus(GrabArtId);
    -movement_in_progress(_);
    !release_artifact.

@release_artifact_discovering
+!release_artifact : not holding(_) <-
    .wait("+holding(_)");
    !release_artifact.

@release_artifact
+!release_artifact[source(Sender)] : holding(ArtifactName) <-
    .print("Agent wants to release the artifact");
    lookupArtifact("envManager", EnvArtId);
    focus(EnvArtId);
    !retrieve_nearest_artifacts_by_type("SnapPoint", Artifacts);
    Artifacts = [First | _];
    stopFocus(EnvArtId);
    !reach_destination(First);
    .wait({ +reached(place, Dest) });
    .nth(0, ArtifactName, Upper); // Get the first character of the string
    .lower_case(Upper, Lower); // Make it lowercase
    .replace(ArtifactName, Upper, Lower, NormalizedName); // replace the first character.
    lookupArtifact(NormalizedName, ArtId);
    focus(ArtId);
    attemptRelease(First).

+released(ArtifactName, SnapPointName) : true <-
    vesna.release(ArtifactName, SnapPointName);
    -holding(ArtifactName);
    .print("Agent released the artifact").

+grabbed(ArtifactName) : true <-
    vesna.grab(ArtifactName);
    +holding(ArtifactName).

-!grab_artifact <-
    .print("Artifact was not available, agent will keep walking.").

{ include("$jacamo/templates/common-cartago.asl") }
{ include("$jacamo/templates/common-moise.asl") }
{ include("$moise/asl/org-obedient.asl") }
