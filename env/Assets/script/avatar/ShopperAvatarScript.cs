using Newtonsoft.Json;
using System;
using WebSocketSharp;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ShopperAvatarScript : AbstractAvatarSocial
{
    public ShopperBeliefs shopperBeliefs;
    private AutonomousWalking autonomousWalking;
    public GameObject[] waypoints;

    protected override void Awake()
    {
        base.Awake();   
        agentFile = "shopper.asl";
        JaCaMoAgentClassPath = "artifact.lib.maselements.AgentMasElement";
        autonomousWalking = (AutonomousWalking) movementModel;
        autonomousWalking.Waypoints = waypoints;
    }

    public override AgentBeliefs AgentBeliefs
    {
        get { return shopperBeliefs; }
    }

}
