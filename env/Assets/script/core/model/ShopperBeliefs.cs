using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[System.Serializable]
public class ShopperBeliefs : AgentBeliefs
{
    public List<ItemToBuy> itemsToBuy;
    public float budget;

    // Constructor to initialize the properties
    public ShopperBeliefs(List<ItemToBuy> itemsToBuy, float budget)
    {
        ItemsToBuy = itemsToBuy;
        Budget = budget;
    }

    // Default constructor
    public ShopperBeliefs()
    {
        ItemsToBuy = new List<ItemToBuy>();
        Budget = 0.0f;
    }

    public float Budget
    {
        get
        {
            return budget;
        }
        set
        {
            budget = value;
        }
    }
    public List<ItemToBuy> ItemsToBuy
    {
        get { return itemsToBuy; }
        set { itemsToBuy = value; }
    }

    // Method to generate beliefs as string to fill .jcm file
    public override string GetBeliefsAsLiterals()
    {
        StringBuilder beliefs = new StringBuilder();
        // Add the budget belief
        beliefs.Append($"budget({Budget})");

        if (itemsToBuy != null && itemsToBuy.Count != 0)
        {
            string temp = "[" + string.Join(", ", itemsToBuy.Select(item => item.GetItemAsLiteral())) + "]";
            beliefs.Append($", shoppingList({temp})");
        }

        if (friends != null && friends.Count != 0)
        {
            string temp = "[" + string.Join(", ", friends.Select(item => item.ToString())) + "]";
            beliefs.Append($", friends({temp})");
        }
        else
        {
            beliefs.Append($", friends([])");
        }        

        return beliefs.ToString();
    }
}