using System;
using System.Collections.Generic;
using UnityEngine;

public static class FFormation
{

    public static void JoinConversation(string agentName, string conversationName, AgentConversations agentConversations)
    //public static void JoinConversation(GameObject agentObj, string conversationName, AgentConversations agentConversations)
    {
        //ConversationObject.AddAgent(agentName, conversationName, agentConversations);
        GameObject agentObj = GameObject.Find(agentName);
        ConversationObject.AddAgent(agentName, conversationName, agentConversations);
        //ChangeFormation(conversationName);
        ChangeFormation(conversationName, agentObj);

    }

    /*public static void ChangeFormation(string conversationName)
    {
        var conv = ConversationObject.ActiveConversations
            .Find(c => c.Conversation.name == conversationName);

        Vector3 center = ConversationObject.GetObjectPosition(conversationName);
        List<string> participants = conv.Participants;
        int count = participants.Count;

        if (count < 3 || count > 5)
        {
            UnityEngine.Debug.LogWarning("FFormation: il numero di partecipanti deve essere tra 3 e 5.");
            return;
        }

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
            }
            else
            {
                float angleStep = 360f / count;
                float angle = i * angleStep * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            }

            agentObj.transform.position = center + offset;

            // DEBUG: stampa posizione e distanza
            float actualDistance = Vector3.Distance(agentObj.transform.position, center);
            UnityEngine.Debug.Log($"Agente '{agentName}' posizionato a distanza {actualDistance:F2} da '{conversationName}', in posizione {agentObj.transform.position}");
        }
    }*/

    public static void ChangeFormation(string conversationName, GameObject newAgentObj)
    {
        var conv = ConversationObject.ActiveConversations
            .Find(c => c.Conversation.name == conversationName);

        Vector3 center = ConversationObject.GetObjectPosition(conversationName);
        List<string> participants = conv.Participants;
        int count = participants.Count;

        if (count < 3 || count > 5)
        {
            UnityEngine.Debug.LogWarning("FFormation: il numero di partecipanti deve essere tra 3 e 5.");
            return;
        }

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
            }
            else
            {
                float angleStep = 360f / count;
                float angle = i * angleStep * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            }

            Vector3 targetPosition = center + offset;

            // Creo un GameObject destinazione con tag "Artifact"
            GameObject destPoint = new GameObject($"dest_{agentName}");
            destPoint.transform.position = targetPosition;
            destPoint.tag = "Artifact";

            // Prendo il componente ShopperAvatarScript (che eredita da AbstractAvatar)
            ShopperAvatarScript avatarScript = agentObj.GetComponent<ShopperAvatarScript>();
            if (avatarScript != null)
            {
                // Enqueue su main thread la chiamata a reachDestination con il nome del GameObject destinazione
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    avatarScript.reachDestination(destPoint.name);
                    //avatarScript.CheckIfReachedFriend(destPoint.name);
                    agentObj.transform.LookAt(destPoint.transform);
                });
            }
            else
            {
                Debug.LogWarning($"GameObject {agentName} non ha componente ShopperAvatarScript.");
            }

            // Debug
            float actualDistance = Vector3.Distance(targetPosition, center);
            Debug.Log($"Agente '{agentName}' deve muoversi a distanza {actualDistance:F2} da '{conversationName}', verso {targetPosition}");
        }
    }

}
