using Newtonsoft.Json;
using System.Linq;
using System;
using UnityEngine;
using WebSocketSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.Experimental;

public class EnvManager : Artifact
{
    public string jcmFilePath = "../mind/supermarket.jcm";
    
    public string JcmFilePath
    {
        get => jcmFilePath;
        set => jcmFilePath = value;
    }
    
    protected override void OnMessage(object sender, MessageEventArgs e)
    {
        string data = e.Data;
        print("Received message: " + data);
        ArtifactMessage message = null;
        try
        {
            message = JsonConvert.DeserializeObject<ArtifactMessage>(data);
        }
        catch (Exception)
        {
            print(data);
            print("Message could not be converted.");
            return;
        }
        try
        {
            string messagePayload = message.MessagePayload;
            switch (messagePayload)
            {
                case "all_artifact_by_type": // Retrieve all artifacts by the type                    
                    retrieveArtifactsByType(message.Param.ToString(), message.AgentName);
                    break;
                case "all_artifact":
                    retrieveAllArtifacts(message.AgentName);
                    break;
                case "nearest":
                    RetrieveNearestArtifactsByType(message.Param.ToString(), message.AgentName);
                    break;

            }
        }
        catch (Exception ex)
        {
            print("Exception occoured OnMessage " + ex);
        }
    }

    private async void retrieveAllArtifacts(string agentName)
    {
        // Create a TaskCompletionSource to await the result
        TaskCompletionSource<string[]> tcs = new TaskCompletionSource<string[]>();
        // Retrieve all artifacts in the game
        UnityMainThreadDispatcher.Instance()
        .Enqueue(() =>
        {
            GameObject[] artifacts = GameObject.FindGameObjectsWithTag("Artifact");
            string[] artifactNames = artifacts
                .Select(artifact => artifact.name) // Select the name of each GameObject
                .ToArray();

            tcs.SetResult(artifactNames);
        });
        string[] artifactNames = await tcs.Task;
        wsChannel.sendMessage(UnityJacamoIntegrationUtil.createAndConvertJacamoMessageIntoJsonString("artifactStrategy",
            null, "artifact_names", agentName, artifactNames));
    }

    private async void retrieveArtifactsByType(string resourceType, string agentName)
    {
        // Create a TaskCompletionSource to await the result
        TaskCompletionSource<string[]> tcs = new TaskCompletionSource<string[]>();
        UnityMainThreadDispatcher.Instance()
        .Enqueue(() =>
        {
            // Retrieve all artifacts in the game
            GameObject[] artifacts = GameObject.FindGameObjectsWithTag("Artifact");
            // Retrieve all artifacts name that have the type specified
            string[] filteredArtifactNames = artifacts
                .Where(artifact => artifact.GetComponent<Artifact>().ArtifactType.ToString() == resourceType)
                .Select(artifact => artifact.name) // Select the name of each GameObject
                .ToArray();
            tcs.SetResult(filteredArtifactNames);
        });
        string[] artifactNames = await tcs.Task;
        wsChannel.sendMessage(UnityJacamoIntegrationUtil.createAndConvertJacamoMessageIntoJsonString("artifactStrategy",
            null, "artifact_names", agentName, artifactNames));
    }

    private async void RetrieveNearestArtifactsByType(string resourceType, string agentName)
    {
        TaskCompletionSource<string[]> tcs = new TaskCompletionSource<string[]>();
        UnityMainThreadDispatcher.Instance()
        .Enqueue(() =>
        {
            GameObject avatar = GameObject.Find(agentName);
            if (avatar == null)
            {
                print($"No avatar named {agentName} found");
                return;
            }
            // Find all objects with the 'Shop' tag
            GameObject[] artifacts = GameObject.FindGameObjectsWithTag("Artifact");
            GameObject[] filteredArtifacts = artifacts.Where(artifact => artifact
            .GetComponent<Artifact>().ArtifactType.ToString() == resourceType).ToArray();

            if (filteredArtifacts.Length == 0)
            {
                Debug.Log("No artifacts found in the scene.");
                return;
            }

            // Create a list to store the shops and their distances
            List<KeyValuePair<GameObject, float>> artifactsWithDistances = new List<KeyValuePair<GameObject, float>>();

            // Calculate the distance from the avatar to each shop and store it
            foreach (GameObject shop in filteredArtifacts)
            {
                float distance = Vector3.Distance(avatar.transform.position, shop.transform.position);
                artifactsWithDistances.Add(new KeyValuePair<GameObject, float>(shop, distance));
            }

            // Sort the list by distance (ascending)
            artifactsWithDistances = artifactsWithDistances.OrderBy(pair => pair.Value).ToList();

            // Extract the sorted GameObjects into a list
            List<GameObject> sortedArtifacts = artifactsWithDistances.Select(pair => pair.Key).ToList();
            tcs.SetResult(sortedArtifacts.Select(artifact => artifact.name).ToArray());
        });
        string[] artifactNames = await tcs.Task;
        wsChannel.sendMessage(UnityJacamoIntegrationUtil.createAndConvertJacamoMessageIntoJsonString("artifactStrategy",
            null, "artifact_names", agentName, artifactNames));
    }
}
