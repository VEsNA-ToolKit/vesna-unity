using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class ConversationRules
{
    public static int partecipants;
    public static string conversation;

    public static GameObject CheckConversation(string targetName, AgentConversations agentConversations, AgentConversations targetConversations, string agentName, AgentBeliefs beliefs){
        if (targetConversations == null || targetConversations.Conversations == null) {
            return null;
        }

        if(targetConversations.Conversations.Count == 0){
            return SocialDistance.StartConversation(targetName, agentConversations, targetConversations, agentName, beliefs);
        }
        else{
            conversation = targetConversations.Conversations[0];
            partecipants = ConversationObject.GetParticipantsCount(conversation);
            if(partecipants < 5){
                UnityEngine.Debug.Log("FFormation!!!");
                FFormation.JoinConversation(agentName, conversation, agentConversations);
            }
            return null;
        }
    }
}