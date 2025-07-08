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
    protected AvatarAnimationController animationController; //aggiunta

    protected override void Awake()
    {
        base.Awake();
        // Set waypoints to follow
        movementModel = GetComponent<MovementModel>();
        animationController = GetComponentInChildren<AvatarAnimationController>();//aggiunta

    }

    protected void resetStoppingDistance()
    {
        agent.stoppingDistance = 1.0f;
    }

    protected IEnumerator CheckIfReachedFriend(string friend)
    {
        while (true)
        {
            // Check if the agent has reached the destination
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    Debug.Log("Agent has reached his friend.");
                    animationController.SetAnimationState("stop");
                    transform.LookAt(GameObject.Find(friend).transform);
                    SendMessageToJaCaMoBrain(UnityJacamoIntegrationUtil
                        .createAndConvertJacamoMessageIntoJsonString("destinationReached", null,
                "reached_friend", null, friend));

                    yield break; // Exit the coroutine
                }
            }
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
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
                            
                        GameObject targetObj = GameObject.Find(walkData.Target); //A
                        ShopperAvatarScript targetAvatar = targetObj.GetComponent<ShopperAvatarScript>(); //A
                        AgentBeliefs targetBeliefs = targetAvatar.AgentBeliefs; //A

                        GameObject conv = SocialDistance.StartConversation(walkData.Target, AgentBeliefs, targetBeliefs); //AA
                        reachDestination(conv.name); //AA
                        StartCoroutine(CheckIfReachedFriend(walkData.Target)); //AA
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
                        SetBaloonText(saysData.Msg);
                        if (animationController != null)
                        {
                            animationController.SetAnimationState("say");
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