@retrieve_shop_artifact
+!retrieve_shop_artifact_by_shop_name(ShopName, ArtifactId) <-
    lookupArtifact(ShopName, ArtifactId).

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