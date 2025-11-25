using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using script.core.model.io;
using script.core.util;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AI;
using WebSocketSharp;

public class AgentAvatarSocial : AgentAvatarWithEyesAndVoice
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
    
    // protected IEnumerator CheckIfReachedFriend(string friend)
    // {
    //     while (true)
    //     {
    //         // Check if the agent has reached the destination
    //         if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    //         {
    //             if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
    //             {
    //                 Debug.Log("Agent has reached his friend.");
    //                 transform.LookAt(GameObject.Find(friend).transform);
    //                 SendMessageToJaCaMoBrain(UnityJacamoIntegrationUtil
    //                     .createAndConvertJacamoMessageIntoJsonString("destinationReached", null,
    //                         "reached_friend", null, friend));
    //
    //                 yield break; // Exit the coroutine
    //             }
    //         }
    //         yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
    //     }
    // }
    
    protected IEnumerator WaitUntilReachedTarget(string target, bool isFriend = false)
    {
        
        // Wait until the agent has a path and is not moving
        yield return new WaitUntil(() =>
            !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
        );
        
        // Searching for the target object with .Find is not efficient, it would be better to either
        // pass the GameObject directly, cache it when setting the destination, or use tags.
        // However, this should work for now.
        var targetObj = GameObject.Find(target);
        if (targetObj)
            transform.LookAt(targetObj.transform);
        
        Debug.Log($"Agent has reached his {(isFriend ? "friend" : "target")}.");
        var messageType = isFriend ? "reached_friend" : "reached_destination";
        var jacamoJsonString = UnityJacamoIntegrationUtil
            .CreateAndConvertJacamoMessageIntoJsonString("destinationReached", null,
                messageType, null, target);
        SendMessageToJaCaMoBrain(jacamoJsonString);
    }

    protected IEnumerator ActivateVisionCone()
    {
        yield return new WaitForSeconds(4.0f);
        EnableDisableVisionCone(true);
    }

    // Unity avatar receives message from jacamo agent
    protected override void OnMessage(object sender, MessageEventArgs e)
    {
        var data = e.Data;
        print("Received message: " + data);
        try
        {
            var message = JsonConvert.DeserializeObject<WsMessage>(data);
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
                case MessageTypes.WsInitialization:
                    HandleConnectionMessage();
                    break;
                case MessageTypes.Walk:
                    print("Agent needs to reach destination.");
                    // Avatar receives the type of artifact to reach
                    WalkData walkData = message.Data.ToObject<WalkData>();

                    if (walkData.Target == null)
                        break;
                    
                    if (walkData.Target == "random")
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(HandleRandomWalk);
                        break;
                    }
                    
                    UnityMainThreadDispatcher.Instance().Enqueue(() => { HandleTargetedWalk(walkData); });
                    break;
                case MessageTypes.Stop:
                    HandleAgentStop();
                    break;
                case MessageTypes.Rotate:
                    var rotateData = message.Data.ToObject<RotateData>();
                    HandleRotateMessage(rotateData);
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
                case MessageTypes.Say:
                    // Avatar receives the type of artifact to reach
                    var saysData = message.Data.ToObject<SaysData>();
                    UnityMainThreadDispatcher.Instance().Enqueue(() => { HandleSpeechBalloon(saysData); });
                    break;
                case MessageTypes.Grab:
                    print("Agent needs to grab an artifact.");
                    var grabData = message.Data.ToObject<GrabData>();
                    UnityMainThreadDispatcher.Instance().Enqueue( () => { HandleArtifactGrab(grabData); } );
                    break;
                case MessageTypes.Release:
                    print("Agent needs to release an artifact.");
                    var releaseData = message.Data.ToObject<ReleaseData>();
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        var artifact = GameObject.Find(releaseData.ArtifactName.Trim('"'));
                        var snapPoint = GameObject.Find(releaseData.SnapPointName.Trim('"'));
                        HandleArtifactRelease(artifact, snapPoint);
                    });
                    break;
                default:
                    print($"Unknown message type for {objInUse.name}");
                    break;
            }
        }
        catch (Exception ex)
        {
            print($"Error: {ex.Message}" );
            print("Message could not be converted.");
        }
    }

    private void HandleTargetedWalk(WalkData walkData)
    {
        SetBaloonText("New destination: " + walkData.Target);
        movementModel.IsStopped = true;
        agent.ResetPath();
        EnableDisableVisionCone(false);
        ReachDestination(walkData.Target);

        // Se il target e' un friend, aggiorna stoppingDistance e avvia il controllo specifico
        if (AgentBeliefs != null && AgentBeliefs.Friends.Contains(walkData.Target))
        {
            agent.stoppingDistance = 8.0f;
            //animationController.SetAnimationState("say");
            ReachDestination(walkData.Target);
                            
            // CheckIfReachedTarget should be the same as CheckIfReachedFriend, but in the eventuality that
            // we want to differentiate them in the future, I'm leaving both the methods available.
            // Uncomment if the other method is not working properly.
            //StartCoroutine(CheckIfReachedFriend(walkData.Target));
                            
            StartCoroutine(WaitUntilReachedTarget(walkData.Target, true));
            if (Mathf.Approximately(agent.stoppingDistance, 8.0f)) //da perfezionare
            {
                animationController.SetAnimationState("stop");
            }
        }
        else
        {
            resetStoppingDistance(); // stoppingDistance = 1.0f
            ReachDestination(walkData.Target);
            StartCoroutine(WaitUntilReachedTarget(walkData.Target));
        }
    }

    private void HandleRandomWalk()
    {
        resetStoppingDistance();
        movementModel.IsStopped = false;
        agent.ResetPath();
        SetBaloonText("Walking");
        movementModel.StartWalking();
        StartCoroutine(ActivateVisionCone());
                            
        if (animationController != null)
        {
            animationController.SetAnimationState("walk"); // o "stop"
        }
    }

    #region Handlers
    
    private void HandleConnectionMessage()
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            print("Connection established for " + objInUse.name);
        });
    }
    
    private void HandleAgentStop()
    {
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
    }
    
    private void HandleRotateMessage(RotateData rotateData)
    {
        if (rotateData.Type == "lookat")
        {
            transform.LookAt(GameObject.Find(rotateData.Target).transform);
            EnableDisableVisionCone(false);
            return;
        }

        print("This rotate is not implemented");
    }
    
    private void HandleSpeechBalloon(SaysData saysData)
    {
        SetBaloonText(saysData.Msg);
        if (animationController != null)
        {
            animationController.SetAnimationState("say");
        }
    }
    
    private void HandleArtifactGrab(GrabData grabData)
    {
        var artifact = GameObject.Find(grabData.Name.Trim('"'));
        HandleArtifactGrab(artifact);
    }
    
    
    
    #endregion
}