using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class SocialDistance
{
    public static float GetStoppingDistance(string targetName, AgentBeliefs beliefs)
    {
        string relationship = GetRelationshipCategory(targetName, beliefs);

       /* if (beliefs == null || string.IsNullOrEmpty(targetName))
            return 1.0f; // default fallback*/

        switch(relationship){
            case "friend":
              UnityEngine.Debug.Log("Personal space");
              return 2.0f; // personal space
            case "neutral":
              UnityEngine.Debug.Log("Social space");
              return 8.0f; // social space

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
}
