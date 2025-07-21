using UnityEngine;

public class RunJacamo : MonoBehaviour
{
    private GameObject[] avatars;
    private GameObject[] environmentArtifacts;

    async void Start()
    {
        avatars = GameObject.FindGameObjectsWithTag("JacamoAgent");
        environmentArtifacts = GameObject.FindGameObjectsWithTag("Artifact");
        // Define agents into the multi agent system
        UnityJacamoIntegrationUtil.ConfigureJcmFile(avatars, environmentArtifacts);

        print(".jcm file configuration done successfully.");

        // Run jacamo application in async
        await UnityJacamoIntegrationUtil.RunJaCaMoApp();

        print("JaCaMo application started successfully.");

        // Start to connect each avatar
        await UnityJacamoIntegrationUtil.StartWebSocketConnections(avatars, environmentArtifacts);

        print("ALL AVATARS AND ENVIRONMENT OBJECTS HAVE BEEN CONNECTED TO JACAMO!!!");
    }
}