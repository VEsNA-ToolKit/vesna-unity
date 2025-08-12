using Newtonsoft.Json;

namespace script.core.model.io
{
    public class ReleaseData
    {
        
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("position")]
        
        public string Position { get; set; }
        [JsonProperty("rotation")]
        
        public string Rotation { get; set; }
        
        public ReleaseData(string name, string position, string rotation)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
        }
    }
}