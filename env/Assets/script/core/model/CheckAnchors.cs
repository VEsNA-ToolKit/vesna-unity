using UnityEngine;

public class CheckAnchors : MonoBehaviour
{
    public string currentAgentName = null;

    public bool IsFree()
    {
        return string.IsNullOrEmpty(currentAgentName);
    }

    public void AssignAgent(string agentName)
    {
        currentAgentName = agentName;
    }

    public void FreeAnchor()
    {
        currentAgentName = null;
    }
}
