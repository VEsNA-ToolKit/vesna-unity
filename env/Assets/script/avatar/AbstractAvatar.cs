using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using script.core.model.io;
using script.core.util;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using WebSocketSharp;

public abstract class AbstractAvatar : AbstractMasElement
{
    public string agentFile; // TODO: find a better way to handle this, like a list of types that is mapped to the agent file
    public AgentBeliefs agentBeliefs;
    public GameObject[] focusedArtifacts;
    public List<GoalEnum> goals;
    protected TextMeshPro nameTextMeshPro;
    protected string jaCaMoAgentClassPath;
    protected NavMeshAgent agent;
    protected string artifactToReach;

    protected virtual void Awake()
    {
        initializeWebSocketConnection(OnMessage);
        // Find the TextMeshPro component in the children of the avatar
        nameTextMeshPro = GetComponentInChildren<TextMeshPro>();
        agent = GetComponent<NavMeshAgent>();

        // Check if we found the TextMeshPro component
        if (nameTextMeshPro != null)
        {
            // Set the text of the TextMeshPro to the avatar's name
            nameTextMeshPro.text = name;
        }
        else
        {
            Debug.LogWarning("TextMeshPro component not found in the avatar's children.");
        }
    }
    
    protected void HandleArtifactGrabAndRelease(GameObject artifact)
    {
        if (artifact != null)
        {
            var artifactSocket = this.transform.Find("Body/artifactHolder");
            artifact.transform.SetParent(artifactSocket);
            
            artifact.transform.localPosition = Vector3.zero; // Reset position to the socket's position
            artifact.transform.localRotation = Quaternion.identity; // Reset rotation to the socket's rotation
            artifact.SetActive(true); // Ensure the artifact is active
            
            print("Holding artifact: " + artifact.name);
        }
        else
        {
            print("No artifact to hold.");
        }
    }

    public GameObject[] FocusedArtifacts
    {
        get { return focusedArtifacts; }
        set { focusedArtifacts = value; }
    }

    public virtual AgentBeliefs AgentBeliefs
    {
        get { return agentBeliefs; }
    }

    public string AgentFile
    {
        get { return agentFile; }
    }

    public List<GoalEnum> Goals
    {
        get { return goals; }

    }

    public string ArtifactToReach
    {
        get { return artifactToReach; }
        set { artifactToReach = value; }
    }


    public string JaCaMoAgentClassPath
    {
        get { return jaCaMoAgentClassPath; }
        set { jaCaMoAgentClassPath = value; }
    }

    protected void reachDestination(string dest)
    {
        agent.isStopped = false;
        agent.SetDestination(GameObject.Find(dest).transform.position);
    }

    public void SendMessageToJaCaMoBrain( string message )
    {
        wsChannel.sendMessage(message);
    }


    // Unity avatar receives message from jacamo agent
    protected virtual void OnMessage(object sender, MessageEventArgs e)
    {
        string data = e.Data;
        print("Received message: " + data);
        WsMessage message = null;
        try
        {
            message = JsonConvert.DeserializeObject<WsMessage>(data);
            switch (message.Type)
            {
                case MessageTypes.WsInitialization:
                    print("Connection established for " + objInUse.name);
                    break;
                case MessageTypes.Walk:
                    print("Agent needs to reach destination.");
                    // Avatar receives the type of artifact to reach
                    WalkData walkData = message.Data.ToObject<WalkData>();
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        reachDestination( walkData.Target );
                    });
                    break;
                default:
                    print("Unknown message type for " + objInUse.name);
                    break;
            }
        }
        catch (Exception)
        {
            print(data);
            print("Message could not be converted.");
            return;
        }
    }
}
