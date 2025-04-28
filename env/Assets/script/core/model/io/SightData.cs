using Newtonsoft.Json;
using System;
using UnityEngine;

public class SightData {

    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }

    public SightData( string type, string model, string name ) {
        Type = type;
        Model = model;
        Name = name;
    }
}
