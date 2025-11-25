using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using script.core.util;
using UnityEngine;
using WebSocketSharp;

[ExecuteAlways]
public class Artifact : AbstractArtifact
{
    // All artifact properties
    //TODO: Find out what subclasses are needed and remove the rest
    // public List<CoffeeInfo> barProperties;
    // public List<FruitInfo> fruitShopProperties;
    // public List<ClothesInfo> dressShopProperties;
    // public bool doorProperties;
    public Dictionary<string, object> Properties { get; private set; }

    private void OnValidate()
    {
        ResolveProperties();
    }

    protected virtual void Awake()
    {
        ResolveProperties();
        propertyNames ??= new List<string>(); // If null, initialize the list
        
        propertyNames.Clear();
        // Retrieve all fields
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.Name != "port" && field.Name != "objInUse")
            {
                propertyNames.Add(field.Name);
            }
        }

        objInUse = gameObject;

        if (Application.IsPlaying(gameObject))
        {
            // Play logic
            // Retrieve the property that belongs to the artifact       
            var artifactPropertyName = artifactType.ToString();
            artifactPropertyName = char.ToLower(artifactPropertyName[0]) + artifactPropertyName[1..] + "Properties";

            // Find the field with the specified name
            var filteredField = Array.Find(fields, f => f.Name == artifactPropertyName);
            if (filteredField != null)
            {
                // Map in JSON the artifact property        
                artifactProperties = EscapeJson(convertObjectIntoJson(filteredField.GetValue(this)));
                Debug.Log("Artifact property: " + artifactProperties.ToString());
            }

            initializeWebSocketConnection(OnMessage);
        }
        else
        {
            // Editor logic
            foreach (var prop in propertyNames)
            {
                print(prop);
            }
        }
    }

    protected virtual void OnMessage(object sender, MessageEventArgs e)
    {
        string data = e.Data;
        
        ArtifactMessage message = null;
        try
        {
            message = JsonConvert.DeserializeObject<ArtifactMessage>(data);
        }
        catch (Exception)
        {
            Debug.LogError(data);
            Debug.LogError("Message could not be converted.");
            return;
        }
        
        try
        {
            string messagePayload = message.MessagePayload;
            switch (messagePayload)
            {
                case "is_grabbable":
                    RetrieveGrabbableStatus(message.AgentName);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{message.AgentName} Artifact] Exception occurred OnMessage " + ex);
        }
    }
    
    private async void RetrieveGrabbableStatus(string artifactName)
    {
        // Create a TaskCompletionSource to await the result
        var tcs = new TaskCompletionSource<bool>();
        await UnityMainThreadDispatcher.Instance()
            .EnqueueAsync(() =>
            {
                var artifact = GameObject.Find(artifactName);
                
                if (artifact == null)
                {
                    Debug.LogError($"Artifact {artifactName} not found.");
                    tcs.SetResult(false);
                    return;
                }
                
                var artifactComponent = artifact.GetComponent<Artifact>();
                if (artifactComponent == null)
                {
                    Debug.LogError($"Artifact component not found on {artifactName}.");
                    tcs.SetResult(false);
                    return;
                }
                
                var grabbable = artifactComponent.isGrabbable;
                tcs.SetResult(grabbable);
            });
        var grabbableStatus = await tcs.Task;

        wsChannel.sendMessage(UnityJacamoIntegrationUtil.CreateAndConvertJacamoMessageIntoJsonString(
            "grabbableStatus", null, "is_grabbable", artifactName, grabbableStatus));
    }
    
    private void ResolveProperties()
    {
        var props = ArtifactResolver.GetAllProperties($"{artifactType}Artifact");
    
        if (props is not { Count: > 0 }) return;
        
        Properties = new Dictionary<string, object>();
        foreach (var prop in props)
        {
            Properties[prop.Name] = prop.Value;
        }
    }

}
