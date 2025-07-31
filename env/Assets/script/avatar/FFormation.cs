using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using WebSocketSharp;

public static class FFormation
{

    public static void JoinConversation(string agentName, string conversationName, AgentConversations agentConversations)
    {
        GameObject agentObj = GameObject.Find(agentName);
        ConversationObject.AddAgent(agentName, conversationName, agentConversations);
        ChangeFormation(conversationName);
    }

    public static void LeaveConversation(string agentName, string conversationName, AgentConversations agentConversations)
    {
        // Rimuove l'agente corrente dalla conversazione
        GameObject agentObj = GameObject.Find(agentName);
        ConversationObject.RemoveAgent(agentName, conversationName, agentConversations);

        // Trova la conversazione aggiornata
        var conv = ConversationObject.ActiveConversations
            .Find(c => c.Conversation.name == conversationName);

        // Se la conversazione è stata completamente rimossa, esce (null)
        if (conv == null)
        {
            Debug.Log($"La conversazione '{conversationName}' è già stata rimossa.");
            return;
        }

        int count = conv.Participants.Count;

        if (count > 1)
        {
            // Se ci sono ancora almeno 2 partecipanti, aggiorna la formazione
            ChangeFormation(conversationName);
        }
        else
        {
            // Se resta solo un partecipante, lo rimuove
            if (conv.Participants.Count == 1)
            {
                string lastAgent = conv.Participants[0];
                GameObject lastAgentObj = GameObject.Find(lastAgent);
                if (lastAgentObj != null)
                {
                    AgentConversations lastAgentConvs = lastAgentObj.GetComponent<AgentConversations>();
                    if (lastAgentConvs != null)
                    {
                        ConversationObject.RemoveAgent(lastAgent, conversationName, lastAgentConvs);
                    }
                    else
                    {
                        Debug.LogWarning($"[LeaveConversation] Il componente AgentConversations non è stato trovato per '{lastAgent}'");
                    }
                }
                else
                {
                    Debug.LogWarning($"[LeaveConversation] Agente '{lastAgent}' non trovato in scena.");
                }
            }


            // Elimina la conversazione
            ConversationObject.DeleteObject(conversationName);
            Debug.Log($"Conversazione '{conversationName}' terminata e rimossa.");
        }

    }



    public static void ChangeFormation(string conversationName)
    {
        var conv = ConversationObject.ActiveConversations
            .Find(c => c.Conversation.name == conversationName);

        Vector3 center = ConversationObject.GetObjectPosition(conversationName);
        List<string> participants = conv.Participants;
        int count = participants.Count;
        UnityEngine.Debug.Log("Participanti alla conversazione: " + count);

        /*if (count < 3)
        {
            //UnityEngine.Debug.LogWarning("FFormation: il numero di partecipanti deve essere tra 3 e 5.");
            if (count == 2){
                
            }
        }*/

        float radius = 5f;

        for (int i = 0; i < count; i++)
        {
            string agentName = participants[i];
            GameObject agentObj = GameObject.Find(agentName);
            if (agentObj == null) continue;

            Vector3 offset;
            if (count == 3)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            } /*else if(count == 2)
            {
                float angle = i * 180f * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            } else if (count < 2){
                ConversationObject.DeleteObject(conversationName);
                return;
            }*/
            else
            {
                float angleStep = 360f / count;
                float angle = i * angleStep * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            }

            Vector3 targetPosition = center + offset;

            // Creo un GameObject destinazione con tag "Artifact"
            GameObject destPoint = GameObject.Find($"dest_{agentName}");
            if (destPoint == null)
            {
                destPoint = new GameObject($"dest_{agentName}");
                destPoint.tag = "Artifact";
            }
            destPoint.transform.position = targetPosition;


            // Prendo il componente ShopperAvatarScript (che eredita da AbstractAvatar)
            ShopperAvatarScript avatarScript = agentObj.GetComponent<ShopperAvatarScript>();
            if (avatarScript != null)
            {
                // Enqueue su main thread la chiamata a reachDestination con il nome del GameObject destinazione
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    Debug.Log($"Enqueue movement for: {agentName}");
                    avatarScript.reachDestination(destPoint.name);

                    AvatarAnimationController animator = agentObj.GetComponentInChildren<AvatarAnimationController>();
                    animator.SetAnimationState("walk");

                    avatarScript.StartCoroutine(avatarScript.CheckIfReachedFriend(destPoint.name));
                    agentObj.transform.LookAt(destPoint.transform);

                    //forse va fatto un resetPath o qualcosa del genere

                });
            }
            else
            {
                Debug.LogWarning($"GameObject {agentName} non ha componente ShopperAvatarScript.");
            }

            float actualDistance = Vector3.Distance(targetPosition, center);
            Debug.Log($"Agente '{agentName}' deve muoversi a distanza {actualDistance:F2} da '{conversationName}', verso {targetPosition}");

            //GameObject.Destroy(destPoint, 100f);
        }
    }

}
