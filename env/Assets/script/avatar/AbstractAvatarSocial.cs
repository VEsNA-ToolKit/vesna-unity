using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using WebSocketSharp;

public class AbstractAvatarSocial : AbstractAvatarWithEyesAndVoice
{
    protected MovementModel movementModel;
    protected AvatarAnimationController animationController; 
    protected AvatarFACS avatarMood;

    [System.NonSerialized]
    public AgentConversations agentConversations;
    [System.NonSerialized]
    public AgentConversations targetConversations;
    private CheckAnchors currentAnchor;

    protected override void Awake()
    {
        base.Awake();
        // Set waypoints to follow
        movementModel = GetComponent<MovementModel>();
        animationController = GetComponentInChildren<AvatarAnimationController>();
        avatarMood = GetComponentInChildren<AvatarFACS>();

        // Associa subito agentConversations
        agentConversations = GetComponent<AgentConversations>();
        if (agentConversations == null)
        Debug.LogWarning($"{name} non ha AgentConversations collegato!");
    }


    public new void SendMessageToJaCaMoBrain( string message )
    {
        wsChannel.sendMessage(message);
    }

    public void resetStoppingDistance()  //questo era protected
    {
        agent.stoppingDistance = 1.0f;
    }

    public IEnumerator CheckIfReachedFriend(string friend)
    {
        GameObject target = GameObject.Find(friend);
        if (target == null)
        {
            Debug.LogWarning($"[CheckIfReachedFriend] Oggetto '{friend}' non trovato.");
            yield break;
        }

        bool isDestinationPoint = friend.StartsWith("dest_") || GameObject.Find(friend)?.tag == "Artifact" || friend.StartsWith("anchor");

        while (true)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath /*|| agent.velocity.sqrMagnitude == 0f*/)
                {
                    Debug.Log($"[CheckIfReachedFriend] Agente ha raggiunto {(isDestinationPoint ? "la destinazione" : "l'amico")}: {friend}");

                    agent.isStopped = true;
                    movementModel.IsStopped = true;
                    animationController.SetAnimationState("stop");

                    Debug.Log("agentConversations: " + agentConversations);
                    Debug.Log("targetConversations: " + targetConversations);

                    if(agentConversations != null && agentConversations.Conversations.Count > 0)
                    {
                        string conversationName = agentConversations.Conversations[0];
                        Vector3 center = ConversationObject.GetObjectPosition(conversationName);
                        StartCoroutine(SmoothLookAt(center));
                    }
                    else
                    {
                        StartCoroutine(SmoothLookAt(target.transform.position)); // fallback
                        Debug.Log("NON STO GUARDANDO IL CENTRO, ma sto guardando " + target);
                        
                    }

                    if (!isDestinationPoint)
                    {
                        SendMessageToJaCaMoBrain(UnityJacamoIntegrationUtil
                            .createAndConvertJacamoMessageIntoJsonString(
                                "destinationReached", null, "reached_friend", null, friend));
                        Debug.Log("DESTINAZIONE: " + friend);
                    }
                    
                    EnableDisableVisionCone(false); 

                    yield break;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

   public IEnumerator SmoothLookAt(Vector3 targetPosition, float speed = 5f)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        while (Vector3.Angle(transform.forward, direction) > 0.1f)
        {
            Vector3 newDir = Vector3.RotateTowards(transform.forward, direction, speed * Time.deltaTime, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDir);

            yield return null;
        }
    }



    protected IEnumerator ActivateVisionCone()
    {
        yield return new WaitForSeconds(4.0f);
        EnableDisableVisionCone(true);
    }

    private IEnumerator WaitForFreeAnchor(GameObject targetObj)
    {
        CheckAnchors[] anchors = targetObj.GetComponentsInChildren<CheckAnchors>();

        while (true)
        {
            foreach (var anchor in anchors)
            {
                if (anchor.IsFree())
                {
                    anchor.AssignAgent(objInUse.name);
                    currentAnchor = anchor;
                    Debug.Log($"[Anchor] {objInUse.name} ha trovato anchor libero: {anchor.name}");

                    movementModel.IsStopped = false;
                    agent.isStopped = false;
                    animationController.SetAnimationState("walk");

                    reachDestination(anchor.name);
                    StartCoroutine(CheckIfReachedFriend(anchor.name));

                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }


    // Unity avatar receives message from jacamo agent
    protected override void OnMessage(object sender, MessageEventArgs e)
    {
        string data = e.Data;
        print("Received message: " + data);
        WsMessage message = null;
        try
        {
            message = JsonConvert.DeserializeObject<WsMessage>(data);
            print("Received Message Type: " + message.Type);
            switch (message.Type)
            {
                case "wsInitialization":
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        print("Connection established for " + objInUse.name);
                    });
                    break;
                case "walk":
                    print("Agent needs to reach destination.");
                    WalkData walkData = message.Data.ToObject<WalkData>();

                    if (walkData.Target == "random")
                    {
                        print("ANDREA CIAO");
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            CheckAnchors[] allAnchors = FindObjectsByType<CheckAnchors>(FindObjectsSortMode.None);
                            foreach (var anchor in allAnchors)
                            {
                                if (anchor.currentAgentName == objInUse.name)
                                {
                                    anchor.FreeAnchor();
                                    Debug.Log($"[Anchor] {objInUse.name} ha liberato anchor: {anchor.name} (cammino verso 'random')");
                                    break;
                                }
                            }

                            if (agentConversations != null && agentConversations.Conversations.Count > 0)
                            {
                                string actualConversation = agentConversations.Conversations[0];
                                FFormation.LeaveConversation(objInUse.name, actualConversation, agentConversations);
                            }

                            resetStoppingDistance();
                            movementModel.IsStopped = false;
                            agent.ResetPath();
                            SetBaloonText("Walking");
                            movementModel.StartWalking();
                            EnableDisableVisionCone(false); 
                            StartCoroutine(ActivateVisionCone());

                            animationController.SetAnimationState("walk");
                            
                        });
                        break;
                    }

                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        if (agentConversations != null && agentConversations.Conversations.Count > 0)
                        {
                            string actualConversation = agentConversations.Conversations[0];
                            FFormation.LeaveConversation(objInUse.name, actualConversation, agentConversations);
                            
                        }

                        SetBaloonText("New destination: " + walkData.Target);
                        movementModel.IsStopped = true;
                        agent.ResetPath();
                        EnableDisableVisionCone(false);
                        Debug.Log("DESTINAZIONE di " + objInUse.name + " : " + walkData.Target);
                        GameObject targetObj = GameObject.Find(walkData.Target);
                        animationController.SetAnimationState("walk");

                        targetConversations = targetObj.GetComponent<AgentConversations>();
                        agentConversations = objInUse.GetComponent<AgentConversations>();

                        GameObject conv = ConversationRules.CheckConversation(walkData.Target, agentConversations, targetConversations, objInUse.name, AgentBeliefs);

                        if (targetObj.CompareTag("Artifact"))
                        {
                            CheckAnchors[] anchors = targetObj.GetComponentsInChildren<CheckAnchors>();
                            bool assigned = false;

                            foreach (var anchor in anchors)
                            {
                                if (anchor.IsFree())
                                {
                                    anchor.AssignAgent(objInUse.name);
                                    reachDestination(anchor.name);
                                    StartCoroutine(CheckIfReachedFriend(anchor.name));
                                    assigned = true;
                                    break;
                                }
                            }

                            if (!assigned)
                            {
                                if (anchors.Length == 0)
                                {
                                    // Non ci sono anchor, vai direttamente alla destinazione dell’Artifact
                                    reachDestination(targetObj.name);
                                    StartCoroutine(CheckIfReachedFriend(targetObj.name));
                                }
                                else
                                {
                                    animationController.SetAnimationState("stop");
                                    StartCoroutine(WaitForFreeAnchor(targetObj));
                                }

                                // qua volendo si può mandare un messaggio a JaCaMo e decide il cervello poi che fare
                                // magari anche in base alla personalità
                            }
                        }

                        if(conv != null){
                            reachDestination(conv.name);
                            StartCoroutine(CheckIfReachedFriend(walkData.Target));
                            Debug.Log("WALK-DATA di " + objInUse.name + " : " + walkData.Target);
                        }

                        
                    });
                    break;

                case "stop":
                    print("Stopping the agent.");
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        SetBaloonText("I'm stopped");
                        movementModel.IsStopped = true;
                        agent.isStopped = true;
                        animationController.SetAnimationState("stop"); 
                        EnableDisableVisionCone(false);
                        // [17.04.25] This goes in the rotate msg
                        // transform.LookAt(GameObject.Find(message.MessagePayload).transform);
                        // EnableDisableVisionCone(false);
                    });
                    break;

                case "rotate":
                    RotateData rotateData = message.Data.ToObject<RotateData>();
                    if (rotateData.Type == "lookat")
                    {
                        transform.LookAt(GameObject.Find(rotateData.Target).transform);
                        EnableDisableVisionCone(false);
                        break;
                    }
                    print("This rotate is not implemented");
                    break;
                // [17.04.25] This case I think is useless, it is just a special case of walking with a target
                // TODO: Add an if in the walk to manage this case
                 /*case "reachFriend":
                     // Avatar receives the type of artifact to reach
                     UnityMainThreadDispatcher.Instance().Enqueue(() =>
                     {
                        movementModel.IsStopped = true;
                        // Delete previous path and reach friend
                        agent.ResetPath();
                        agent.stoppingDistance = 8.0f;
                        EnableDisableVisionCone(false);
                        reachDestination(message.MessagePayload);
                        StartCoroutine(CheckIfReachedFriend(message.MessagePayload));
                     });
                     break;*/
                case "say":
                    // Avatar receives the type of artifact to reach
                    SaysData saysData = message.Data.ToObject<SaysData>();
                    if(!string.IsNullOrEmpty(saysData.Mood)){
                        Debug.Log($"[Say] Msg: {saysData.Msg}, Mood: {saysData.Mood}");
                    }

                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        animationController.SetAnimationState("say"); 
                        SetBaloonText(saysData.Msg);
                        Debug.Log(objInUse.name + " sta parlando");
                        
                        
                       if(!string.IsNullOrEmpty(saysData.Mood)){
                            if(avatarMood == null)
                                Debug.LogWarning("avatarMood is null!");
                            else {
                                string mood = saysData.Mood.Trim().Trim('"').ToLowerInvariant();
                                Debug.Log($"[Say] Applying mood '{mood}' to {objInUse.name}");
                                 switch (mood)
                                {
                                    case "happy":
                                        avatarMood.SetHappiness();
                                        break;
                                    case "sad":
                                        avatarMood.SetSadness();
                                        break;
                                    case "surprised":
                                        avatarMood.SetSurprise();
                                        break;
                                    case "angry":
                                    case "anger":
                                        avatarMood.SetAnger();
                                        break;
                                    case "disgust":
                                        avatarMood.SetDisgust();
                                        break;
                                    case "fear":
                                        avatarMood.SetFear();
                                        break;
                                    case "neutral":
                                    case "normal":
                                        avatarMood.SetNeutral();
                                        break;
                                    default:
                                        Debug.LogWarning($"[Say] Mood '{mood}' not handled in FacialExpressionController.");
                                        break;
                                    }
                                }
                        }                               
                    });
                break;
                default:
                    print("Unknown message type for " + objInUse.name);
                    break;
            }
        }
        catch (Exception ex)
        {
            print("Error: " + ex.Message);
            print("Message could not be converted.");
            return;
        }
    }

}