using Newtonsoft.Json;
using System;
using WebSocketSharp;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

public class ShopperAvatarScript : AbstractAvatarSocial
{
    public ShopperBeliefs shopperBeliefs;
    private AutonomousWalking autonomousWalking;
    public GameObject[] waypoints;

    protected override void Awake()
    {
        base.Awake();   
        agentFile = string.IsNullOrEmpty(agentFile) ? "shopper.asl" : agentFile;
        JaCaMoAgentClassPath = "artifact.lib.maselements.AgentMasElement";
        autonomousWalking = (AutonomousWalking) movementModel;
        if (waypoints is { Length: > 0 })
            autonomousWalking.Waypoints = waypoints;
    }

    public override AgentBeliefs AgentBeliefs => shopperBeliefs;
}
