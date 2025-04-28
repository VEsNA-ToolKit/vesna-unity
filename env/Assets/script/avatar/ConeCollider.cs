using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class ConeCollider : MonoBehaviour
{

    private NavMeshAgent agent;
    bool reachedArtifact = false;
    GameObject root;
    ShopperAvatarScript mainAvatarScript;

    public bool ReachedArtifact
    {
        get { return reachedArtifact; }
        set { reachedArtifact = value; }
    }

    private void Awake()
    {
        // Retrieve the root gameobject that represent the avatar
        root = transform.parent.transform.parent.gameObject;
        // Retrieve the avatar baloon
        mainAvatarScript = root.GetComponent<ShopperAvatarScript>();

    }

    void OnTriggerEnter(Collider other)
    {
        GameObject obj = other.gameObject;
        if (obj.layer == LayerMask.NameToLayer("artifact"))
        {
            Debug.Log("Agent " + root.name + " has seen the artifact " + other.name);
            mainAvatarScript.SetBaloonText("Artifact seen: " + other.name.FirstCharacterToLower());
            // Prepare artifact info to send
            ArtifactInfo artifactInfo = new ArtifactInfo
            {
                ArtifactName = other.name,
                ArtifactType = retrieveArtifactType(obj),                
            };

            root.GetComponent<ShopperAvatarScript>().SendMessageToJaCaMoBrain(UnityJacamoIntegrationUtil.createAndConvertJacamoMessageIntoJsonString("eyes", null,
                "artifactSeen", null, artifactInfo));
        }
        else if (obj.layer == LayerMask.NameToLayer("agent"))
        {
            Debug.Log("Agent " + root.name + " has met another avatar: " + obj.transform.parent.name);
            mainAvatarScript.SetBaloonText("Agent seen: " + obj.transform.parent.name);
            root.GetComponent<ShopperAvatarScript>().SendMessageToJaCaMoBrain(UnityJacamoIntegrationUtil.createAndConvertJacamoMessageIntoJsonString("eyes", null,
                "agentSeen", null, obj.transform.parent.name));
        }
        else if (obj.layer == LayerMask.NameToLayer("sphere"))
        {
            obj.gameObject.GetComponent<Renderer>().material.color = Color.blue;
        }
    }

    private static string retrieveArtifactType(GameObject other)
    {
        // Get artifact type 
        GenericArtifactType artType = other.GetComponent<GenericArtifactType>();
        if (artType == null)
        {
            throw new Exception("Artifact type script is null");
        }
        return artType.GetShopType().ToString().ToLower();
    }
}
