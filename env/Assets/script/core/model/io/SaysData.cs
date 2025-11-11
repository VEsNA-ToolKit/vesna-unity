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
    [JsonProperty("emotion")]
    public string? Emotion { get; set; }

    public SaysData() {} //aggiunto costruttore vuoto per far partire il case say 

    public SaysData( string msg ) {
        Msg = msg;
    }

    public SaysData( string to, string msg ) {
        Recipient = to;
        Msg = msg;
    }
    
    public SaysData( string to, string msg, string emotion) {
        Recipient = to;
        Msg = msg;
        Emotion = emotion;
    }

    public SaysData( string perf, string to, string msg, string emotion){
        Performative = perf;
        Recipient = to;
        Msg = msg;
        Emotion = emotion;
    } 

}