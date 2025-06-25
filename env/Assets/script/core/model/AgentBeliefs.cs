using System.Collections.Generic;
using UnityEngine;

public abstract class AgentBeliefs
{

    public List<string> friends = new List<string>();
    // List of acquaintances
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

    [Header("Personality")] // Personality List
    public PersonalityProfile personalityProfile = new PersonalityProfile();

    // Method to generate beliefs as string to fill .jcm file
    public abstract string GetBeliefsAsLiterals();

}