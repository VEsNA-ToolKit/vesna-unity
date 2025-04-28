using Newtonsoft.Json;
using System;
using UnityEngine;

public class RotateData {

    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("direction")]
    public string? Direction { get; set; } 
    [JsonProperty("target")]
    public string? Target { get; set; } 
    [JsonProperty("id")]
    public int? Id { get; set; } 

    public RotateData( string type, string arg ) {
        Type = type;
        if ( type == "direction" )
            Direction = arg;
        else if ( type == "lookat" )
            Target = arg;
    }

    public RotateData( string type, string target, int id ) {
        Type = type;
        Target = target;
        Id = id;
    }
}