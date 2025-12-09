using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;
using script.core.util;
using Unity.VisualScripting;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    private void OnDestroy()
    {
        var grabbable = GetXRGrabbable();

        grabbable.selectEntered.RemoveAllListeners();
        grabbable.selectExited.RemoveAllListeners();
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
                Debug.Log("Artifact property: " + artifactProperties);
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
        var data = e.Data;
        
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
            var messagePayload = message.MessagePayload;
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
            if (prop.Name == "canBeGrabbedByHumanUser")
            {
                CreateVRGrabbableComponent();
            }
            
            Properties[prop.Name] = prop.Value;
        }
    }
    private void CreateVRGrabbableComponent()
    {
        Debug.Log("[DEBUG] Creating VR Grabbable Component");

        var grabbable = GetXRGrabbable();
        SetupInteractionManager(grabbable);

        // Make sure the collider is set up for interaction
        var componentCollider = gameObject.GetComponent<Collider>();
        if (componentCollider == null)
        {
            gameObject.AddComponent<BoxCollider>();
            Debug.Log("[DEBUG] BoxCollider added to Artifact GameObject for XR grabbing.");
        }
        
        Debug.Log("[DEBUG] VR Grabbable successfully created.");
        
        // Listeners for grabbed and release events, we'll send the wsMessages from here
        grabbable.selectEntered.AddListener(OnGrabbedVR);
        grabbable.selectExited.AddListener(OnReleasedVR);
    }

    private void OnGrabbedVR(SelectEnterEventArgs arg)
    {
        Debug.Log("[DEBUG] Artifact grabbed in VR.");
        
        // Send a Brain Message to JaCaMo
        var msg = new BrainMessage("user", gameObject.name, "grabbed", null);
        wsChannel.sendMessage(JsonConvert.SerializeObject(msg));
        
    }

    private void OnReleasedVR(SelectExitEventArgs args)
    {
        Debug.Log("[DEBUG] Artifact released in VR.");
        
        // Send a Brain Message to JaCaMo
        var msg = new BrainMessage("user", gameObject.name, "grabbed", null);
        wsChannel.sendMessage(JsonConvert.SerializeObject(msg));
    }
    
    private void SetupInteractionManager(XRGrabInteractable grabbable)
    {
        grabbable.interactionManager = FindFirstObjectByType<XRInteractionManager>();
        if (grabbable.interactionManager != null) return;
        
        grabbable.interactionManager = gameObject.AddComponent<XRInteractionManager>();
        Debug.Log("[DEBUG] XRInteractionManager component added to Artifact GameObject.");
    }
    
    private XRGrabInteractable GetXRGrabbable()
    {
        var grabbable = gameObject.GetComponent<XRGrabInteractable>();
        if (grabbable != null) return grabbable;

        grabbable = gameObject.AddComponent<XRGrabInteractable>();
        Debug.Log("[DEBUG] XRGrabInteractable component added to Artifact GameObject.");

        return grabbable;
    }
    
}