using Newtonsoft.Json;
using System;
using UnityEngine;

public class SaysData {

    [JsonProperty("msg")]
    public string Msg { get; set; }
    [JsonProperty("recipient")]
    public string? Recipient { get; set; } 
    [JsonProperty("performative")]
    public string? Performative { get; set; } 

    public SaysData( string msg ) {
        Msg = msg;
    }

    public SaysData( string to, string msg ) {
        Recipient = to;
        Msg = msg;
    }

    public SaysData( string perf, string to, string msg ){
        Performative = perf;
        Recipient = to;
        Msg = msg;
    } 

}