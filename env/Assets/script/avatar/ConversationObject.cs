using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class ConversationObject
{
    private static int conversationCount = 1;
    public static List<ConversationData> ActiveConversations = new List<ConversationData>();

    public static GameObject CreateConversation(string agentName, string targetName, AgentConversations agentConversations, AgentConversations targetConversations){
        
        GameObject conversationObj = new GameObject($"conversation {conversationCount}");
        ActiveConversations.Add(new ConversationData
        {
        Conversation = conversationObj,
        Participants = new List<string> { agentName, targetName }
        });

        conversationCount++;

        return conversationObj;
    }

    public static int GetParticipantsCount(string conversation){

        foreach (var conv in ActiveConversations)
        {
            if (conv.Conversation.name == conversation)
            {
                return conv.Participants.Count;
            }
        }

        return 0;
    }

    public static Vector3 GetObjectPosition(string conversationName)
    {
        var data = ActiveConversations.Find(c => c.Conversation.name == conversationName);
        return data?.Conversation.transform.position ?? Vector3.zero;
    }

    public static void AddAgent(string agentName, string conversationName, AgentConversations agentConversations)
    {
        var conversation = ActiveConversations.Find(conv => conv.Conversation.name == conversationName);
        if (conversation != null)
        {
            if (!conversation.Participants.Contains(agentName))
            {
                conversation.Participants.Add(agentName);
                agentConversations.Conversations.Add(conversationName);
                UnityEngine.Debug.Log($"Add agent '{agentName}' to conversation '{conversationName}'.");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"AddAgentToConversation: conversation '{conversationName}' not found .");
        }
    }

    public static void RemoveAgent(string agentName, string conversationName, AgentConversations agentConversations)
    {
        var conversation = ActiveConversations.Find(conv => conv.Conversation.name == conversationName);
        if (conversation != null)
        {
            if (conversation.Participants.Contains(agentName))
            {
                conversation.Participants.Remove(agentName);
                agentConversations.Conversations.Remove(conversationName);
                UnityEngine.Debug.Log($"Removed agent '{agentName}' from conversation '{conversationName}'.");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"RemoveAgentFromConversation: agent '{agentName}' is not a participant of conversation '{conversationName}'.");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"RemoveAgentFromConversation: conversation '{conversationName}' not found.");
        }
    }

    public static void DeleteObject(string conversationName)
    {
        var conversationData = ActiveConversations.Find(conv => conv.Conversation.name == conversationName);
        if (conversationData != null)
        {
            ActiveConversations.Remove(conversationData);
            UnityEngine.Object.Destroy(conversationData.Conversation);
            UnityEngine.Debug.Log($"Deleted conversation object '{conversationName}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"DeleteObject: conversation '{conversationName}' not found.");
        }
    }


}
