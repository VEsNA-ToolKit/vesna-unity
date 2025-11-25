using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FruitInfo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public FruitEnum itemName;
    public double price;
    public int quantity;
}