using UnityEngine;

public class AvatarFACS : MonoBehaviour
{
    private SkinnedMeshRenderer skinnedMesh;
    private int smileIndex = -1;

    void Start()
    {
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();

        if (skinnedMesh != null)
            smileIndex = skinnedMesh.sharedMesh.GetBlendShapeIndex("mouthSmile");
    }

    public void SetSkinnerMesh(string mood)
    {
        if (skinnedMesh == null)
        {
            Debug.LogWarning("SkinnedMeshRenderer not found!");
            return;
        }

        if (smileIndex == -1)
        {
            Debug.LogWarning("Blendshape 'mouthSmile' not found!");
            return;
        }

        switch (mood?.Trim().Trim('"').ToLowerInvariant())
        {
            case "happy":
                skinnedMesh.SetBlendShapeWeight(smileIndex, 100f);
                Debug.Log("The agent is happy");
                break;
            case "sad":
                skinnedMesh.SetBlendShapeWeight(smileIndex, 0f);
                Debug.Log("The agent is sad");
                break;
            case "normal":
                skinnedMesh.SetBlendShapeWeight(smileIndex, 50f);
                Debug.Log("The agent is normal");
                break;
            default:
                Debug.LogWarning($"Mood '{mood}' is not handled");
                break;
        }
    }
}
