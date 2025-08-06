using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class SocialDistance
{
    public static PersonalityProfile personalityProfile = new PersonalityProfile();

    public static float GetStoppingDistance(string targetName, AgentBeliefs beliefs)
    {
        string relationship = GetRelationshipCategory(targetName, beliefs);
        float personality = GetPersonalityDistance(beliefs);

        switch(relationship){
            case "friend":
              UnityEngine.Debug.Log("Personal zone");
              UnityEngine.Debug.Log("check personality: " + personality);
              return 2.0f + personality; // zona personale
            case "neutral":
              UnityEngine.Debug.Log("Social zone");
              UnityEngine.Debug.Log("check personality: " + personality);
              return 10.0f + personality; // zona sociale

            default:
              return 15.0f;
        }
    }

    public static float GetPersonalityDistance(AgentBeliefs agentBeliefs){
        float estroversione = agentBeliefs.personalityProfile.Estroversione;

        if(estroversione < 0.5){
            return -0.5f;
        } 
        else 
        {
            return 0.5f;
        }
    }

    public static string GetRelationshipCategory(string targetName, AgentBeliefs beliefs)
    {
        if (beliefs == null || string.IsNullOrEmpty(targetName))
            return "unknown";

        if (beliefs.Friends.Contains(targetName))
            return "friend";

        if (beliefs.Neutrals.Contains(targetName))
            return "neutral";

        return "unknown";
    }

    public static GameObject StartConversation(string targetName, AgentConversations agentConversations, AgentConversations targetConversations, string agentName, AgentBeliefs beliefs)
    {
        GameObject conversationObj = ConversationObject.CreateConversation(agentName, targetName, agentConversations, targetConversations);

        agentConversations.Conversations.Add(conversationObj.name);
        targetConversations.Conversations.Add(conversationObj.name);

        float distance = GetStoppingDistance(targetName, beliefs);
        GameObject targetObj = GameObject.Find(targetName);
        Vector3 direction = targetObj.transform.forward;
        conversationObj.transform.position = targetObj.transform.position + direction * distance;
        UnityEngine.Debug.Log("posizione top");

        return conversationObj;
    }

    public static float SetDistance(string targetName, AgentBeliefs beliefs){
        float distance = GetStoppingDistance(targetName, beliefs);

        return distance;
    }
}
