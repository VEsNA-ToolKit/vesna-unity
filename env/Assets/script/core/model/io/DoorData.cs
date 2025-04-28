using Newtonsoft.Json;
using System;
using UnityEngine;

public class DoorData {

    [JsonProperty("status")]
    public bool Status { get; set; }

    public DoorData( bool status ) {
        Status = status;
    }
}
