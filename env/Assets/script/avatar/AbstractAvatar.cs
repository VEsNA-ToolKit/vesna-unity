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
    
    protected void HandleArtifactGrab(GameObject artifact)
    {
        if (artifact != null)
        {
            var artifactSocket = this.transform.Find("Body/artifactHolder");
            artifact.transform.SetParent(artifactSocket);
            
            artifact.transform.localPosition = Vector3.zero; // Reset position to the socket's position
            artifact.transform.localRotation = Quaternion.identity; // Reset rotation to the socket's rotation
            
            print("Holding artifact: " + artifact.name);
        }
        else
        {
            print("No artifact to hold.");
        }
    }
    
    protected void HandleArtifactRelease(GameObject artifact, GameObject snapPoint)
    {
        if (artifact != null && snapPoint != null)
        {
            artifact.transform.SetParent(null); // Detach from avatar
            
            Collider surfaceCollider = snapPoint.GetComponent<Collider>();
            Renderer movingRenderer = artifact.GetComponent<Renderer>();
            
            Vector3 newPos = snapPoint.transform.position; // XZ position of snap point
            float surfaceTopY = surfaceCollider.bounds.max.y;
            float movingBottomY = movingRenderer.bounds.min.y;
            float yOffset = surfaceTopY - movingBottomY;

            newPos.y += yOffset;
            artifact.transform.position = newPos;
            artifact.transform.rotation = snapPoint.transform.rotation;
            
            print("Released artifact: " + artifact.name + " to snap point: " + snapPoint.name);
        }
        else
        {
            print("No artifact to release or no snap point provided.");
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

    protected void ReachDestination(string dest)
    {
        agent.isStopped = false;


        var destObject = GameObject.Find(dest);
        if (destObject == null)
        {
            Debug.LogError($"Destination is null.");
            return;
        }
        
        agent.SetDestination(destObject.transform.position);
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
                        ReachDestination( walkData.Target );
                    });
                    break;
                default:
                    Debug.LogError("Unknown message type for " + objInUse.name);
                    break;
            }
        }
        catch (Exception)
        {
            print(data);
            Debug.LogError("Message could not be converted.");
            return;
        }
    }
}
