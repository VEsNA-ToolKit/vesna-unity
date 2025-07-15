using System.Collections.Generic;
using UnityEngine;

public abstract class AgentBeliefs
{
    public List<string> friends = new List<string>();
    public List<string> neutrals = new List<string>();

    public List<string> Friends
    {
        get
        {
            return friends;
        }
    }

    public List<string> Neutrals
    {
        get
        {
            return neutrals;
        }
    }

    [Header("Personality")]
    public PersonalityProfile personalityProfile = new PersonalityProfile();

    
    public abstract string GetBeliefsAsLiterals();

}