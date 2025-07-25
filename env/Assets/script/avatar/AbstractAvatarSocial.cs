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

    [System.NonSerialized]
    public AgentConversations agentConversations;
    [System.NonSerialized]
    public AgentConversations targetConversations;

    protected override void Awake()
    {
        base.Awake();
        // Set waypoints to follow
        movementModel = GetComponent<MovementModel>();
        animationController = GetComponentInChildren<AvatarAnimationController>();
    }

    public void SendMessageToJaCaMoBrain( string message )
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

        bool isDestinationPoint = friend.StartsWith("dest_");

        while (true)
        {
            // Controlla se ha finito il path ed è fermo vicino al target
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    Debug.Log($"[CheckIfReachedFriend] Agente ha raggiunto {(isDestinationPoint ? "la destinazione" : "l'amico")}: {friend}");

                    // Ferma animazione e guarda il target
                    animationController.SetAnimationState("stop");
                    transform.LookAt(GameObject.Find(friend).transform);


                    if (isDestinationPoint)
                    {
                        GameObject obj = GameObject.Find(friend);
                        transform.LookAt(obj.transform);
                    }
                    else
                    {
                        // Invia messaggio al brain che ha raggiunto l'amico
                        SendMessageToJaCaMoBrain(UnityJacamoIntegrationUtil
                            .createAndConvertJacamoMessageIntoJsonString(
                                "destinationReached", null, "reached_friend", null, friend));
                    }

                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f); 
        }
    }


    protected IEnumerator ActivateVisionCone()
    {
        yield return new WaitForSeconds(4.0f);
        EnableDisableVisionCone(true);
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
                // [17.04.25] startWalking is now part of walk with random target
                // case "startWalking":
                //     // Avatar receives the type of artifact to reach
                //     UnityMainThreadDispatcher.Instance().Enqueue(() =>
                //     {
                //         resetStoppingDistance();
                //         movementModel.IsStopped = false;
                //         agent.ResetPath();
                //         SetBaloonText("Walking");
                //         movementModel.StartWalking();
                //         StartCoroutine(ActivateVisionCone());
                //     });
                //     break;
                case "wsInitialization":
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        print("Connection established for " + objInUse.name);
                    });
                    break;
                case "walk":
                    print("Agent needs to reach destination.");
                    // Avatar receives the type of artifact to reach
                    WalkData walkData = message.Data.ToObject<WalkData>();
                    if (walkData.Target == "random")
                    {
                        print("ANDREA CIAO");
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            resetStoppingDistance();
                            movementModel.IsStopped = false;
                            agent.ResetPath();
                            SetBaloonText("Walking");
                            movementModel.StartWalking();
                            StartCoroutine(ActivateVisionCone());
                            //aggiunta
                            if (animationController != null)
                            {
                                animationController.SetAnimationState("walk"); // o "stop"
                            }
                            //fino a qui
                        });
                        break;
                    }
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        SetBaloonText("New destination: " + walkData.Target);
                        movementModel.IsStopped = true;
                        agent.ResetPath();
                        EnableDisableVisionCone(false);
                        reachDestination(walkData.Target);
                            
                        GameObject targetObj = GameObject.Find(walkData.Target); 
                        targetConversations = targetObj.GetComponent<AgentConversations>(); 
                        agentConversations = objInUse.GetComponent<AgentConversations>();

                        GameObject conv = ConversationRules.CheckConversation(walkData.Target, agentConversations, targetConversations, objInUse.name, AgentBeliefs); ///B
                        
                        if(conv != null){
                            reachDestination(conv.name); 
                            StartCoroutine(CheckIfReachedFriend(walkData.Target)); 
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
                        //aggiunta
                        if (animationController != null)
                        {
                            animationController.SetAnimationState("stop"); 
                        }
                        //fino a qui
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
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        if (animationController != null)
                        {
                            animationController.SetAnimationState("say"); 
                        }
                        SetBaloonText(saysData.Msg);
                        
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