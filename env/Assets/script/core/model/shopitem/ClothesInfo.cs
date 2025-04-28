using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ClothesInfo
{
    // annotation used to send the enum name and not the integer index
    [JsonConverter(typeof(StringEnumConverter))]
    public ClothesEnum itemName;
    public double price;
    public int quantity;
}