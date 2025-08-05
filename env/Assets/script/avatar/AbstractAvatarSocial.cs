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
    private CheckAnchors currentAnchor;

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

        float maxWaitTime = 10f; // sicurezza: massimo 10 secondi di attesa
        float elapsedTime = 0f;

        while (elapsedTime < maxWaitTime)
        {
            if (!agent.pathPending)
            {
                float distance = Vector3.Distance(agent.transform.position, target.transform.position);
                //Debug.Log($"[CheckIfReachedFriend] agent.remainingDistance = {agent.remainingDistance}, distance = {distance}, velocity = {agent.velocity.magnitude}");

                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    // In alcuni casi Unity non resetta il path, lo forziamo
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f || distance < 0.5f)
                    {
                        Debug.Log($"[CheckIfReachedFriend] Agente ha raggiunto {(isDestinationPoint ? "la destinazione" : "l'amico")}: {friend}");

                        agent.ResetPath(); // forza la fine del movimento
                        animationController.SetAnimationState("stop");

                        // Guarda verso il centro della conversazione (se presente)
                        if (agentConversations != null && agentConversations.Conversations.Count > 0)
                        {
                            string conversationName = agentConversations.Conversations[0];
                            Vector3 center = ConversationObject.GetObjectPosition(conversationName);
                            StartCoroutine(SmoothLookAt(center));
                        }
                        else
                        {
                            StartCoroutine(SmoothLookAt(target.transform.position));
                        }

                        if (!isDestinationPoint)
                        {
                            SendMessageToJaCaMoBrain(UnityJacamoIntegrationUtil
                                .createAndConvertJacamoMessageIntoJsonString(
                                    "destinationReached", null, "reached_friend", null, friend));
                        }

                        EnableDisableVisionCone(true);
                        yield break;
                    }
                }
            }

            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning($"[CheckIfReachedFriend] TIMEOUT: L'agente '{gameObject.name}' non ha raggiunto '{friend}' dopo {maxWaitTime} secondi.");
    }



    //ADD for a more realistic turn
    protected IEnumerator SmoothLookAt(Vector3 targetPosition, float duration = 0.5f)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
        float time = 0f;

        while (time < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation;
    }


    protected IEnumerator ActivateVisionCone()
    {
        yield return new WaitForSeconds(4.0f);
        EnableDisableVisionCone(true);
    }

    // serve per controllare quando si libera l'anchor 
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
                    reachDestination(anchor.name);
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
                    WalkData walkData = message.Data.ToObject<WalkData>();

                    if (walkData.Target == "random")
                    {
                        print("ANDREA CIAO");
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            // Chekc per controllare se esiste un anchor associato all'agente
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
                            StartCoroutine(ActivateVisionCone());

                            if (animationController != null)
                            {
                                animationController.SetAnimationState("walk");
                            }
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
                        //reachDestination(walkData.Target);
                        Debug.Log("DESTINAZIONE di " + objInUse.name + " : " + walkData.Target);
                        GameObject targetObj = GameObject.Find(walkData.Target);

                        if (animationController != null)
                        {
                            animationController.SetAnimationState("walk");
                        }

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
                                    assigned = true;
                                    break;
                                }
                            }

                            if (!assigned)
                            {
                                Debug.Log($"[Anchor] Nessun anchor libero ora per {objInUse.name}, in attesa...");
                                StartCoroutine(WaitForFreeAnchor(targetObj));

                                // qua volendo si può mandare un messaggio a JaCaMo e decide il cervello poi che fare
                                // magari anche in base alla personalità
                            }
                        }


                        targetConversations = targetObj.GetComponent<AgentConversations>();
                        agentConversations = objInUse.GetComponent<AgentConversations>();

                        GameObject conv = ConversationRules.CheckConversation(walkData.Target, agentConversations, targetConversations, objInUse.name, AgentBeliefs);
                        if (conv != null)
                        {
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
                        if (animationController != null)
                        {
                            animationController.SetAnimationState("stop"); 
                        }
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