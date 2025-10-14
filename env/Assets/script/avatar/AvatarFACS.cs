using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AvatarFACS : MonoBehaviour
{
    public SkinnedMeshRenderer faceMesh;    //face renderer with blend shape
    private Dictionary<string, int> blendShapeIndices = new Dictionary<string, int>();  //internal mapping for blend shape's names

    //routine to fill the blend shape dictionary and to set neutral expression
    void Start()
    {
        CacheBlendShapeIndices();
        SetNeutral();
    }

    //routine to reference the blend shapes by their name
    void CacheBlendShapeIndices()
    {
        int count = faceMesh.sharedMesh.blendShapeCount;
        for (int i = 0; i < count; i++)
        {
            string name = faceMesh.sharedMesh.GetBlendShapeName(i);
            blendShapeIndices[name] = i;
        }
    }

    //routine to set the blend shaoe value called
    void SetBlendShape(string name, float value)
    {
        if (blendShapeIndices.TryGetValue(name, out int index))
        {
            faceMesh.SetBlendShapeWeight(index, value);
        }
        else
        {
            Debug.LogWarning($"Blendshape '{name}' not found!");
        }
    }

    //routine to set neutral expression
    public void SetNeutral()
    {
        foreach (var kvp in blendShapeIndices)
            faceMesh.SetBlendShapeWeight(kvp.Value, 0f);
    }
    
		//AUS CONNECTED TO BIG 5
    public void SetNeuroticism()
    {
        SetNeutral();
        SetBlendShape("browDownLeft", 0.3f); // AU4
        SetBlendShape("browDownRight", 0.3f);
        SetBlendShape("browOuterUpLeft", 0.4f); // AU2
        SetBlendShape("browOuterUpRight", 0.4f);
        SetBlendShape("mouthStretchLeft", 0.5f); // AU20
        SetBlendShape("mouthStretchRight", 0.5f);
        Debug.Log("The agent is neurotic");
    }

    public void SetExtraversion()
    {
        SetNeutral();
        SetBlendShape("cheekSquintLeft", 0.3f); // AU6
        SetBlendShape("cheekSquintRight", 0.3f);
        SetBlendShape("mouthSmile", 0.4f); // AU12
        SetBlendShape("mouthFunnel", 0.1f); // AU25
        Debug.Log("The agent is extroverted");
    }

    public void SetConscientiousness()
    {
        SetNeutral();
        SetBlendShape("browDownLeft", 0.3f); // AU4
        SetBlendShape("browDownRight", 0.3f);
        SetBlendShape("mouthFrownLeft", 0.4f); // AU15
        SetBlendShape("mouthFrownRight", 0.4f);
        SetBlendShape("cheekSquintLeft", 0.8f); // AU11
        SetBlendShape("cheekSquintRight", 0.8f);
        SetBlendShape("mouthDimpleLeft", 0.5f);
        SetBlendShape("mouthDimpleRight", 0.5f);
        Debug.Log("The agent is conscientious");
    }

    public void SetAgreeableness()
    {
        SetNeutral();
        SetBlendShape("cheekSquintLeft", 0.3f); // AU6
        SetBlendShape("cheekSquintRight", 0.3f);
        SetBlendShape("jawOpen", 0.05f); // AU25
        SetBlendShape("mouthSmileLeft", 0.2f); // AU12
        SetBlendShape("mouthSmileRight", 0.2f);

        // Occhiolino
        StartCoroutine(WinkOnce("eyeBlinkRight", 1f, 0.25f, 0.5f));
    }

    //coroutine to make the avatar blink
    private IEnumerator WinkOnce(string blendShapeName, float amount, float closeDuration, float holdTime)
    {
        float t = 0f;
        while (t < closeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, amount, t / closeDuration);
            SetBlendShape(blendShapeName, a);
            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        t = 0f;
        while (t < closeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(amount, 0f, t / closeDuration);
            SetBlendShape(blendShapeName, a);
            yield return null;
        }

        SetBlendShape(blendShapeName, 0f);
    }

    public void SetHighOpenness()
    {
        SetNeutral();
        SetBlendShape("noseSneerLeft", 0.4f); // AU9
        SetBlendShape("noseSneerRight", 0.4f);
        SetBlendShape("eyeWideLeft", 0.4f); // AU5
        SetBlendShape("eyeWideRight", 0.4f);
        SetBlendShape("jawOpen", 0.2f); // AU26
    }

    public void SetLowOpenness()
    {
        SetNeutral();
        SetBlendShape("eyeLookUpLeft", 0.5f);
        SetBlendShape("eyeLookUpRight", 0.5f);
        SetBlendShape("eyeLookInRight", 0.5f); 
        SetBlendShape("eyeLookOutLeft", 1f);
        SetBlendShape("mouthClose", 0.1f);
    }
    
    
    //AUS CONNECTED TO EMOTIONS
    public void SetHappiness()
    {
        SetNeutral();
        SetBlendShape("mouthSmile", 0.4f); //AU12
        SetBlendShape("cheekSquintLeft", 0.5f); //AU6
        SetBlendShape("cheekSquintRight", 0.5f);
        SetBlendShape("mouthOpen", 0.1f); //for a more realistic smile
        Debug.Log("The agent is happy");
    }

    public void SetSadness()
    {
        SetNeutral();
        SetBlendShape("browInnerUp", 0.3f); //AU1
        SetBlendShape("browDownLeft", 0.3f); //AU4
        SetBlendShape("browDownRight", 0.3f);
        SetBlendShape("mouthShrugLower", 0.7f); //AU15
        SetBlendShape("mouthFrownLeft", 0.4f); //for a more realistic frown smile
        SetBlendShape("mouthFrownRight", 0.4f);
    }

    public void SetSurprise()
    {
        SetNeutral();
        SetBlendShape("browInnerUp", 0.4f); //AU1
        SetBlendShape("browOuterUpLeft", 0.3f); //AU2
        SetBlendShape("browOuterUpRight", 0.3f);
        SetBlendShape("eyeWideLeft", 0.2f); //AU5
        SetBlendShape("eyeWideRight", 0.2f); 
        SetBlendShape("jawOpen", 0.5f); //A26
    }

    public void SetFear()
    {
        SetNeutral();
        SetBlendShape("browInnerUp", 0.3f); //AU1
        SetBlendShape("browOuterUpLeft", 0.3f); //AU2
        SetBlendShape("browOuterUpRight", 0.3f);
        SetBlendShape("eyeWideLeft", 0.4f); //AU5
        SetBlendShape("eyeWideRight", 0.4f);
        SetBlendShape("eyeSquintLeft", 0.3f); //AU7
        SetBlendShape("eyeSquintRight", 0.3f);         
        SetBlendShape("mouthStretchLeft", 0.7f); //AU20
        SetBlendShape("mouthStretchRight", 0.7f);
        SetBlendShape("jawOpen", 0.2f); //A26
    }

    public void SetAnger() {
        
        SetNeutral();
        SetBlendShape("browDownLeft", 0.3f); //AU4
        SetBlendShape("browDownRight", 0.3f);
        SetBlendShape("eyeWideLeft", 0.2f); //AU2
        SetBlendShape("eyeWideRight", 0.2f);
        SetBlendShape("eyeSquintLeft", 0.3f); //AU7
        SetBlendShape("eyeSquintRight", 0.3f);
        SetBlendShape("mouthShrugLower", 0.7f); //for more realistic tightened lips
    }

    public void SetDisgust() {
        
        SetNeutral();
        SetBlendShape("noseSneerLeft", 0.5f); //AU9
        SetBlendShape("noseSneerRight", 0.5f);
        SetBlendShape("mouthUpperUpLeft", 0.4f); //AU10
        SetBlendShape("mouthUpperUpRight", 0.4f);
        SetBlendShape("eyeSquintLeft", 0.2f); //to squint the eyes
        SetBlendShape("eyeSquintRight", 0.2f);
        SetBlendShape("browDownLeft", 0.1f); //for lowerer eyebrows
        SetBlendShape("browDownRight", 0.1f);
    }
}
