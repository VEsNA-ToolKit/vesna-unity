using System;
using UnityEngine;

public static class SocialDistance
{
    public static PersonalityProfile personalityProfile = new PersonalityProfile();

    public static float GetStoppingDistance(string targetName, AgentBeliefs beliefs)
    {
        string relationship = GetRelationshipCategory(targetName, beliefs);
        float personalityDistance = GetPersonalityDistance(beliefs);

        float finalDistance = 0f;

        switch(relationship)
        {
            case "friend":
                // Gli amici sono più vicini
                finalDistance = personalityDistance - 0.5f; // Distanza per amici (zona personale)
                UnityEngine.Debug.Log("Friend");
                UnityEngine.Debug.Log("check personality distance: " + personalityDistance);
                break;

            case "neutral":
                // I neutrali hanno una distanza maggiore
                finalDistance = personalityDistance + 1.0f; // Distanza per neutri (zona sociale)
                UnityEngine.Debug.Log("Neutral");
                UnityEngine.Debug.Log("check personality distance: " + personalityDistance);
                break;

            default:
                finalDistance = 15.0f; // Distanza generica per relazioni sconosciute
                break;
        }

        return Mathf.Clamp(finalDistance, 0.5f, 3.5f); // Clamp per limitare la distanza tra la zona intima e la zona sociale
    }

    public static float GetPersonalityDistance(AgentBeliefs agentBeliefs)
    {
        float neuroticismExtraversion = agentBeliefs.personalityProfile.Neuroticism_Extraversion;
        float conscientiousnessAgreeableness = agentBeliefs.personalityProfile.Conscientiousness_Agreeableness;
        float openness = agentBeliefs.personalityProfile.Openness;

        float distance = 0f;

        if (neuroticismExtraversion >= 0.5f)
        {
            // Estroversione alta, distanza ridotta
            distance += 0.5f; // 0.5 m (zona intima)
        }
        else
        {
            // Neuroticismo alto, distanza maggiore
            distance += 2.0f; // 2 m (zona sociale)
        }

        if (conscientiousnessAgreeableness >= 0.5f)
        {
            // Affabilità alta, distanza ridotta
            distance += 0.7f; // 0.7 m (zona personale)
        }
        else
        {
            // Coscienziosità alta, distanza rispettosa
            distance += 1.2f; // 1.2 m (zona personale)
        }

        if (openness >= 0.5f)
        {
            // Apertura alta, distanza ridotta
            distance += 0.8f; // 0.8 m (zona personale)
        }
        else
        {
            // Apertura bassa, distanza maggiore
            distance += 1.2f; // 1.2 m (zona personale)
        }

        // Restituisci la distanza complessiva, limitata a valori realistici tra 0.5 m e 3.5 m
        return Mathf.Clamp(distance, 0.5f, 3.5f);
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

    public static float GetDistance(string targetName, AgentBeliefs beliefs){
        float distance = GetStoppingDistance(targetName, beliefs);

        return distance;
    }
}
