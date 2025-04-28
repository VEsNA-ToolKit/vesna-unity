using Newtonsoft.Json;
using System;
using UnityEngine;

public class WalkData {

    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("target")]
    public string Target { get; set; } 

    public WalkData( string type, string target ) {
        Type = type;
        Target = target;
    }
}