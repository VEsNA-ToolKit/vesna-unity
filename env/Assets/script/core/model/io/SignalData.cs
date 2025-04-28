using Newtonsoft.Json;
using System;
using UnityEngine;

public class SignalData {

    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("status")]
    public string Status { get; set; } 
    [JsonProperty("reason")]
    public string Reason { get; set; } 

    public SignalData( string type, string status, string reason ) {
        Type = type;
        Status = status;
        Reason = reason;
    }
}