
using Newtonsoft.Json;
using System;
using UnityEngine;

public class ArtsData {

    [JsonProperty("names")]
    public string[] Names { get; set; }

    public ArtsData( string[] names ) {
        Names = names;
    }
}