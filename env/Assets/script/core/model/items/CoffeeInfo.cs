using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CoffeeInfo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public CoffeeEnum itemName;
    public double price;
    public int quantity;
}
