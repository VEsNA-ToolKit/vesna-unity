using Newtonsoft.Json;

namespace script.core.model.io
{
    public class ReleaseData
    {
        
        [JsonProperty("name")]
        public string ArtifactName { get; set; }
        
        [JsonProperty("snap_name")]
        public string SnapPointName { get; set; }
        
        public ReleaseData(string artifactName, string snapPointName)
        {
            ArtifactName = artifactName;
            SnapPointName = snapPointName;
        }
    }
}