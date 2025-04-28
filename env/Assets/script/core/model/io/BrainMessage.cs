using Newtonsoft.Json;
using System;
using UnityEngine;


[Serializable]
public class BrainMessage {

    [JsonProperty("sender")]
    public string Sender { get; set; }
    [JsonProperty("receiver")]
    public string Receiver { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("data")]
    public object Data { get; set; }
    
    public BrainMessage( string sender, string receiver, string type, object data )
    {
        Sender = sender;
        Receiver = receiver;
        Type = type;
        Data = data;
    }


}
