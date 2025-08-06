using System.Collections.Generic;
using UnityEngine;

public static class FFormation
{
    private const float RADIUS = 5f;

    public static void JoinConversation(string agentName, string conversationName, AgentConversations agentConversations)
    {
        GameObject agentObj = GameObject.Find(agentName);
        if (agentObj == null)
        {
            Debug.LogWarning($"[JoinConversation] Agente '{agentName}' non trovato.");
            return;
        }

        ConversationObject.AddAgent(agentName, conversationName, agentConversations);
        ChangeFormation(conversationName);
    }

    public static void LeaveConversation(string agentName, string conversationName, AgentConversations agentConversations)
    {
        GameObject agentObj = GameObject.Find(agentName);
        ConversationObject.RemoveAgent(agentName, conversationName, agentConversations);

        var conv = ConversationObject.ActiveConversations.Find(c => c.Conversation.name == conversationName);
        if (conv == null)
        {
            Debug.Log($"La conversazione '{conversationName}' è già stata rimossa.");
            return;
        }

        if (conv.Participants.Count > 1)
        {
            ChangeFormation(conversationName);
        }
        else
        {
            RemoveLastParticipant(conv, conversationName);
        }
    }

    private static void RemoveLastParticipant(ConversationData conv, string conversationName)
    {
        if (conv.Participants.Count == 1)
        {
            string lastAgent = conv.Participants[0];
            GameObject lastObj = GameObject.Find(lastAgent);
            if (lastObj != null)
            {
                var lastConvs = lastObj.GetComponent<AgentConversations>();
                if (lastConvs != null)
                {
                    ConversationObject.RemoveAgent(lastAgent, conversationName, lastConvs);
                }
            }
        }

        ConversationObject.DeleteObject(conversationName);
        Debug.Log($"Conversazione '{conversationName}' terminata e rimossa.");
    }

    public static void ChangeFormation(string conversationName)
    {
        var conv = ConversationObject.ActiveConversations.Find(c => c.Conversation.name == conversationName);
        if (conv == null) return;

        Vector3 center = ConversationObject.GetObjectPosition(conversationName);
        List<string> participants = conv.Participants;
        int count = participants.Count;

        Debug.Log("Partecipanti alla conversazione: " + count);

        for (int i = 0; i < count; i++)
        {
            string agentName = participants[i];
            GameObject agentObj = GameObject.Find(agentName);
            if (agentObj == null) continue;

            Vector3 offset = ComputeOffset(participants, i, count);
            Vector3 targetPosition = center + offset;

            GameObject destination = GetOrCreateDestination(agentName, targetPosition);
            MoveAgent(agentObj, agentName, destination);

            float actualDistance = Vector3.Distance(targetPosition, center);
            Debug.Log($"Agente '{agentName}' deve muoversi a distanza {actualDistance:F2} da '{conversationName}', verso {targetPosition}");
        }
    }

    private static Vector3 ComputeOffset(List<string> participants, int index, int count)
    {
        if (count == 3)
        {
            float angle = index * 120f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * RADIUS;
        }
        else if (count == 2)
        {
            string agentA = participants[0];
            string agentB = participants[1];

            GameObject objA = GameObject.Find(agentA);
            GameObject objB = GameObject.Find(agentB);
            if (objA == null || objB == null) return Vector3.zero;

            var beliefsA = objA.GetComponent<ShopperAvatarScript>()?.AgentBeliefs;
            var beliefsB = objB.GetComponent<ShopperAvatarScript>()?.AgentBeliefs;
            if (beliefsA == null || beliefsB == null) return Vector3.zero;

            float distAB = SocialDistance.SetDistance(agentB, beliefsA);
            float distBA = SocialDistance.SetDistance(agentA, beliefsB);

            Vector3 direction = (objB.transform.position - objA.transform.position).normalized;
            return (participants[index] == agentA) ? -direction * (distAB / 2f) : direction * (distBA / 2f);
        }
        else
        {
            float angleStep = 360f / count;
            float angle = index * angleStep * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * RADIUS;
        }
    }

    private static GameObject GetOrCreateDestination(string agentName, Vector3 position)
    {
        GameObject dest = GameObject.Find($"dest_{agentName}");
        if (dest == null)
        {
            dest = new GameObject($"dest_{agentName}");
            dest.tag = "Artifact";
        }

        dest.transform.position = position;
        return dest;
    }

    private static void MoveAgent(GameObject agentObj, string agentName, GameObject destination)
    {
        ShopperAvatarScript avatar = agentObj.GetComponent<ShopperAvatarScript>();
        if (avatar == null)
        {
            Debug.LogWarning($"GameObject '{agentName}' non ha componente ShopperAvatarScript.");
            return;
        }

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log($"Enqueue movement for: {agentName}");
            avatar.reachDestination(destination.name);

            var animator = agentObj.GetComponentInChildren<AvatarAnimationController>();
            animator?.SetAnimationState("walk");

            avatar.StartCoroutine(avatar.CheckIfReachedFriend(destination.name));
            agentObj.transform.LookAt(destination.transform);
        });
    }
}
