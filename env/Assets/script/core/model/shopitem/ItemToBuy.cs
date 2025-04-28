using System.Collections;
using UnityEngine;

[System.Serializable]
public class ItemToBuy
{
    public ShopItemEnum item;
    public int quantity;

    public int Quantity
    {
        get { return quantity; }
        set { quantity = value; }
    }

    public ShopItemEnum Item
    {
        get { return item; }
        set { item = value; }
    }


    // Rerturn the representation of the item as a literal for JaCaMo - item(apple, 5)
    public string GetItemAsLiteral()
    {        
        return $"item({item.ToString().ToLowerInvariant()}, {quantity})";        
    }
}
