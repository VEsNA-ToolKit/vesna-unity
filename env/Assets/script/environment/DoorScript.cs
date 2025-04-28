using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

public class DoorScript : Artifact
{
    private int flag = -1;
    private bool isSupermarketOpen;

    protected override void Awake()
    {
        base.Awake();

        isSupermarketOpen = doorProperties;
        // Initialize property
        ArtifactProperties = doorProperties.ToString();

        if (isSupermarketOpen)
        {
            // Change door color to blue  
            GetComponent<Renderer>().material.color = Color.blue;
        }
        else
        {
            // Stay closed - color red
            GetComponent<Renderer>().material.color = Color.red;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.IsPlaying(gameObject))
        {
            // supermarket is open and message has not been sent yet to JACaMo
            if (doorProperties && flag != 0)
            {
                // Change door color to blue  
                GetComponent<Renderer>().material.color = Color.blue;
                wsChannel.sendMessage(UnityJacamoIntegrationUtil
                .createAndConvertJacamoMessageIntoJsonString("supermarketDoorStatus", null, null, null, true));
                flag = 0;
            }
            // supermarket is closed and message has not been sent yet to JACaMo
            if (!doorProperties && flag != 1)
            {
                // Stay closed
                GetComponent<Renderer>().material.color = Color.red;
                wsChannel.sendMessage(UnityJacamoIntegrationUtil
                .createAndConvertJacamoMessageIntoJsonString("supermarketDoorStatus", null, null, null, false));
                flag = 1;
            }
        }
    }
}