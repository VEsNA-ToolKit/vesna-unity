using UnityEngine;
using WebSocketSharp;

public class CounterScript : Artifact
{
    private int counter = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("JacamoAgent"))
        {
            counter++;
            // Print the name of the object that entered the trigger
            Debug.Log("Trigger detected with " + other.gameObject.name);
            // Send message to JaCaMo
            wsChannel.sendMessage(UnityJacamoIntegrationUtil
                .createAndConvertJacamoMessageIntoJsonString("counter", null, "assign_number", other.name, null));
        }
    }
}
