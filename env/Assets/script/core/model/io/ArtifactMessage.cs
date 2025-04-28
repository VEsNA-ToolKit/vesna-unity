using Newtonsoft.Json;
using System;
using UnityEngine;


[Serializable]
public class ArtifactMessage
{
    [JsonProperty("messageType")]
    public string MessageType { get; set; }
    [JsonProperty("messagePayload")]
    public string MessagePayload { get; set; }
    [JsonProperty("agentEvent")]
    public string AgentEvent { get; set; }
    [JsonProperty("agentName")]
    public string AgentName { get; set; }
    [JsonProperty("param")]
    public object Param { get; set; }
    
    public ArtifactMessage(string messageType, string messagePayload, string agentEvent, string agentName, object param)
    {
        MessageType = messageType;
        MessagePayload = messagePayload;
        AgentEvent = agentEvent;
        AgentName = agentName;
        Param = param;
    }

}