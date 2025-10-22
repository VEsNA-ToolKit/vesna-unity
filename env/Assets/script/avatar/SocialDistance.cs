using System;
using UnityEngine;

public static class SocialDistance
{
    public static PersonalityProfile personalityProfile = new PersonalityProfile();

    public static float GetStoppingDistance(string targetName, AgentBeliefs beliefs)
    {
        string relationship = GetRelationshipCategory(targetName, beliefs);
        float personalityDistance = GetPersonalityDistance(beliefs, targetName);

        float finalDistance = 0f;

        switch (relationship)
        {
            case "friend":
                // Friends tend to stand closer (personal space)
                finalDistance = personalityDistance - 0.5f;
                Debug.Log($"[SocialDistance] Friend → base distance {personalityDistance:F2}");
                break;

            case "neutral":
                // Neutral relationships keep a bit more distance (social space)
                finalDistance = personalityDistance + 1.0f;
                Debug.Log($"[SocialDistance] Neutral → base distance {personalityDistance:F2}");
                break;

            default:
                // Unknown relationship → default larger distance
                finalDistance = personalityDistance + 5.0f;
                Debug.Log($"[SocialDistance] Unknown → base distance {personalityDistance:F2}");
                break;
        }

        // Clamp distance between realistic bounds (intimate to social space)
        return Mathf.Clamp(finalDistance, 0.5f, 10.0f);
    }

    public static float GetPersonalityDistance(AgentBeliefs agentBeliefs, string targetName)
    {
        // Find the target GameObject
        GameObject targetObj = GameObject.Find(targetName);
        if (targetObj == null)
        {
            Debug.LogWarning($"[SocialDistance] Target '{targetName}' not found in the scene.");
            return 1.5f;
        }

        var targetScript = targetObj.GetComponent<ShopperAvatarScript>();
        if (targetScript == null || targetScript.AgentBeliefs == null)
        {
            Debug.LogWarning($"[SocialDistance] Target '{targetName}' has no valid AgentBeliefs.");
            return 1.5f;
        }

        var targetBeliefs = targetScript.AgentBeliefs;

        // --- AGENT PERSONALITY ---
        float neuroticismExtraversion = agentBeliefs.personalityProfile.Neuroticism_Extraversion;
        float conscientiousnessAgreeableness = agentBeliefs.personalityProfile.Conscientiousness_Agreeableness;
        float openness = agentBeliefs.personalityProfile.Openness;

        // --- TARGET PERSONALITY ---
        float neuroticismExtraversion_target = targetBeliefs.personalityProfile.Neuroticism_Extraversion;
        float conscientiousnessAgreeableness_target = targetBeliefs.personalityProfile.Conscientiousness_Agreeableness;
        float openness_target = targetBeliefs.personalityProfile.Openness;

        float distance = 0f;
        float distance_target = 0f;

        // --- DISTANCE BASED ON AGENT PERSONALITY ---
        distance += (neuroticismExtraversion >= 0.5f) ? 0.5f : 2.0f;
        distance += (conscientiousnessAgreeableness >= 0.5f) ? 0.7f : 1.2f;
        distance += (openness >= 0.5f) ? 0.8f : 1.2f;

        // --- DISTANCE BASED ON TARGET PERSONALITY ---
        distance_target += (neuroticismExtraversion_target >= 0.5f) ? 0.5f : 2.0f;
        distance_target += (conscientiousnessAgreeableness_target >= 0.5f) ? 0.7f : 1.2f;
        distance_target += (openness_target >= 0.5f) ? 0.8f : 1.2f;

        // --- AVERAGE BETWEEN AGENT AND TARGET PERSONALITY DISTANCES ---
        float finalDistance = (distance + distance_target) / 2f;

        // Clamp for realistic social distance range
        return Mathf.Clamp(finalDistance, 0.5f, 10.0f);
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

        Debug.Log($"[SocialDistance] Conversation between {agentName} and {targetName} → distance {distance:F2}");

        return conversationObj;
    }
}
