using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class AvatarBody : MonoBehaviour
{
    NavMeshAgent agent;
    GameObject root;
    ShopperAvatarScript mainAvatarScript;
    AvatarAnimationController animationController; 
    string artifactReached = "";

    // Start is called before the first frame update
    void Awake()
    {
        root = transform.parent.gameObject;
        mainAvatarScript = root.GetComponent<ShopperAvatarScript>();
        agent = GetComponent<NavMeshAgent>();
        animationController = GetComponent<AvatarAnimationController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void reachDestination(string dest)
    {
        agent.isStopped = false;
        agent.SetDestination(GameObject.Find(dest).transform.position);
        animationController.SetAnimationState("walk");
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject hit = other.gameObject;

        // Se l'oggetto è un anchor, risali al padre
        if (hit.name.ToLower().Contains("anchor") && hit.transform.parent != null)
            hit = hit.transform.parent.gameObject;

        if (!hit.name.Contains("counter") && hit.tag == "Artifact")
        {
            string dest = hit.name.FirstCharacterToLower();
            Debug.Log("Agent " + root.name + " reached destination " + dest);
            mainAvatarScript.SetBaloonText("Reached destination: " + dest);
            mainAvatarScript.SendMessageToJaCaMoBrain(
                UnityJacamoIntegrationUtil.createAndConvertJacamoMessageIntoJsonString(
                    "destinationReached", null, "reached_destination", null, dest
                )
            );
            
            animationController.SetAnimationState("stop");
            artifactReached = dest;
            mainAvatarScript.EnableDisableVisionCone(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject ex = other.gameObject;
        if (ex.name.ToLower().Contains("anchor") && ex.transform.parent != null)
            ex = ex.transform.parent.gameObject;

        if (!artifactReached.Equals("") && ex.name.FirstCharacterToLower().Equals(artifactReached))
        {
            mainAvatarScript.EnableDisableVisionCone(true);
            artifactReached = "";
        }
    }
    
}
