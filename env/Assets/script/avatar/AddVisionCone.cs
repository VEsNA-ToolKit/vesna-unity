using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddVisionCone : MonoBehaviour
{
    public FOVEnum fovSize;
    private GameObject visionCone;
    private GameObject anchorVisionCone;

    private void Awake()
    {
        visionCone = Resources.Load<GameObject>($"FOVModels/{fovSize.ToString()}");
        // Retrieve agent anchor to attacch vision cone
        anchorVisionCone = transform.Find("anchorVisionCone").gameObject;
    }

    // Start is called before the first frame update
    void Start()
    {        
        // Instantiate the prefab as a child of the parentObject
        if (visionCone != null)
        {
            // Instantiate the prefab as a child of the specified parent
            GameObject instance = Instantiate(visionCone, anchorVisionCone.transform);

            // Optionally reset the position and rotation relative to the parent
            instance.transform.localPosition = Vector3.zero;           
            instance.transform.localRotation = Quaternion.identity;

            // Add a MeshCollider to the instance
            MeshCollider meshCollider = instance.AddComponent<MeshCollider>();

            // Optionally, set properties for the MeshCollider
            if (meshCollider != null)
            {
                meshCollider.convex = true;
                meshCollider.isTrigger = true;
                meshCollider.includeLayers = LayerMask.GetMask("artifact", "agent");
            }

            //Add script to manage collision
            instance.AddComponent<ConeCollider>();
        }
        else
        {
            Debug.LogWarning($"Prefab '{fovSize.ToString()}' not found in Resources/Prefabs!");
        }
    }

    public enum FOVEnum
    {
        FOV30,
        FOV50,
        FOV70,
        FOV90,
        FOV110,
        FOV130,
        FOV150,
        FOV170
    }
}
