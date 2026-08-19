using Newtonsoft.Json;

namespace script.core.model.io
{
    public class GrabData
    {
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        
        public GrabData(string name)
        {
            Name = name;
        }
    }
}