using Newtonsoft.Json;
using System;
using UnityEngine;

public class MoveData {

    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }

    public MoveData( string type, string name ) {
        Type = type;
        Name = name;
    }
}
