#nullable enable
using Newtonsoft.Json;
using System;
using UnityEngine;

public class SaysData {

    [JsonProperty("msg")]
    public string? Msg { get; set; }
    [JsonProperty("recipient")]
    public string? Recipient { get; set; } 
    [JsonProperty("performative")]
    public string? Performative { get; set; } 
    [JsonProperty("mood")]
    public string? Mood { get; set; }

    public SaysData() {} //aggiunto costruttore vuoto per far partire il case say 

    public SaysData( string msg ) {
        Msg = msg;
    }

    public SaysData( string to, string msg ) {
        Recipient = to;
        Msg = msg;
    }
    
    public SaysData( string to, string msg, string mood) {
        Recipient = to;
        Msg = msg;
        Mood = mood;
    }

    public SaysData( string perf, string to, string msg, string mood){
        Performative = perf;
        Recipient = to;
        Msg = msg;
        Mood = mood;
    } 

}