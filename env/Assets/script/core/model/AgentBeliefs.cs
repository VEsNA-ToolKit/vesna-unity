using System.Collections.Generic;

public abstract class AgentBeliefs
{

    public List<string> friends = new List<string>();

    public List<string> Friends
    {
        get
        {
            return friends;
        }
    }

    // Method to generate beliefs as string to fill .jcm file
    public abstract string GetBeliefsAsLiterals();

}