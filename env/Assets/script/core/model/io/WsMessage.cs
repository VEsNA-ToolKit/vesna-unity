using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;


[Serializable]
public class WsMessage {

    [JsonProperty("sender")]
    public string Sender { get; set; }
    [JsonProperty("receiver")]
    public string Receiver { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("data")]
    public JObject Data { get; set; }
    
    // public WsMessage( string sender, string receiver, string type, object data )
    // {
    //     Sender = sender;
    //     Receiver = receiver;
    //     Type = type;
    //     Data = data;
    // }


}
