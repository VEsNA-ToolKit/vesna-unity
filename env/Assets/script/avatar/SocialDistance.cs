using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class SocialDistance
{
    private static int conversationCount = 1;

    public static float GetStoppingDistance(string targetName, AgentBeliefs beliefs)
    {
        string relationship = GetRelationshipCategory(targetName, beliefs);

       /* if (beliefs == null || string.IsNullOrEmpty(targetName))
            return 1.0f; // default fallback*/

        switch(relationship){
            case "friend":
              UnityEngine.Debug.Log("Personal space");
              return 2.0f; 
            case "neutral":
              UnityEngine.Debug.Log("Social space");
              return 15.0f; 

            default:
              return 15.0f;
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

    public static GameObject StartConversation(string targetName, AgentBeliefs beliefs, AgentBeliefs targetBeliefs /*, GameObject objInUse*/)
    {
        GameObject conversationObj = new GameObject($"conversation {conversationCount}");
        conversationCount++;

        beliefs.Conversations.Add(conversationObj.name);
        targetBeliefs.Conversations.Add(conversationObj.name);

        float distanza = GetStoppingDistance(targetName, beliefs);
        GameObject targetObj = GameObject.Find(targetName);
        Vector3 direzione = targetObj.transform.forward;
        conversationObj.transform.position = targetObj.transform.position + direzione * distanza;
        UnityEngine.Debug.Log("posizione top");

        return conversationObj;
    }
}
