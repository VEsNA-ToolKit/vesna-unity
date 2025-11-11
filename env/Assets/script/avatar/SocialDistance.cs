using System;
using UnityEngine;

public static class SocialDistance
{
    public static float GetStoppingDistance(string targetName, AgentBeliefs beliefs)
    {
        string relationship = GetRelationshipCategory(targetName, beliefs);
        float deltaD = GetPersonalityOffset(beliefs, targetName);

        // Base distances based on relationship type (in meters)
        float baseDistance = relationship switch
        {
            "friend"  => 0.82f,  // Personal zone
            "neutral" => 2.43f,  // Social zone
            _         => 3.5f    // Default = public/unknown zone
        };

        // Combine base distance with personality offset
        float finalDistance = baseDistance + deltaD;

        // Clamp to avoid unrealistic values
        return Mathf.Clamp(finalDistance, 0.4f, 10.5f);
    }

    public static float GetPersonalityOffset(AgentBeliefs agentBeliefs, string targetName)
    {
        GameObject targetObj = GameObject.Find(targetName);
        if (targetObj == null)
        {
            Debug.LogWarning($"[SocialDistance] Target '{targetName}' not found in the scene.");
            return 0f;
        }

        var targetScript = targetObj.GetComponent<ShopperAvatarScript>();
        if (targetScript == null || targetScript.AgentBeliefs == null)
        {
            Debug.LogWarning($"[SocialDistance] Target '{targetName}' has no valid AgentBeliefs.");
            return 0f;
        }

        var targetBeliefs = targetScript.AgentBeliefs;
        var aP = agentBeliefs.personalityProfile;
        var tP = targetBeliefs.personalityProfile;

        // Combined personality values (average between the two agents)
        float N = (aP.Neuroticism + tP.Neuroticism) / 2f;
        float E = (aP.Extraversion + tP.Extraversion) / 2f;
        float C = (aP.Conscientiousness + tP.Conscientiousness) / 2f;
        float A = (aP.Agreeableness + tP.Agreeableness) / 2f;
        float O = (aP.Openness + tP.Openness) / 2f;
        
        // Weighted personality effect on distance
        float deltaD =
            (-1.0f * A) +   
            (-0.8f * E) +   
            (-0.4f * O) +   
            (0.6f * N) +   
            (0.3f * C);     

        // Clamp to keep the offset within realistic and visible range
        return Mathf.Clamp(deltaD, -1.2f, 1.2f);
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
        GameObject conversationObj = ConversationObject.CreateConversation(
            agentName, targetName, agentConversations, targetConversations);

        agentConversations.Conversations.Add(conversationObj.name);
        targetConversations.Conversations.Add(conversationObj.name);

        float distance = GetStoppingDistance(targetName, beliefs);
        GameObject targetObj = GameObject.Find(targetName);
        GameObject agentObj = GameObject.Find(agentName);
        Vector3 direction = (agentObj.transform.position - targetObj.transform.position).normalized;

        float scaleFactor = targetObj.transform.localScale.x; // Assuming uniform scaling
        Debug.Log("Scale factor = " + scaleFactor);
        float adjustedDistance = (distance * scaleFactor) + 2.0f;;
        Debug.Log("adjustedDistance = " + adjustedDistance);
        conversationObj.transform.position = targetObj.transform.position + (direction * adjustedDistance);

        Debug.Log($"[SocialDistance] Conversation {agentName} ↔ {targetName} → distance {distance:F2} m");
        return conversationObj;
    }
}
