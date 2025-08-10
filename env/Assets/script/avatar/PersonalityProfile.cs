using UnityEngine;

[System.Serializable]
public class PersonalityProfile
{
    [Range(0f, 1f)] public float Neuroticism_Extraversion;
    [Range(0f, 1f)] public float Conscientiousness_Agreeableness;
    [Range(0f, 1f)] public float Openness;

    public void Validate()
    {
        
        float total = Neuroticism_Extraversion + 
            Conscientiousness_Agreeableness + Openness;

        Debug.Log("Total: " + total);
        
        if(total > 1f){
            DefaultValues();
        }

        CheckValues();
    }

    public void DefaultValues(){ 
        Neuroticism_Extraversion = 0f;
        Conscientiousness_Agreeableness = 0f;
        Openness = 0f;
    }

    public void CheckValues(){
        if(Neuroticism_Extraversion > 0.5){
            Conscientiousness_Agreeableness = 0f;
            Openness = 0f;
        }

        if(Conscientiousness_Agreeableness > 0.5){
            Neuroticism_Extraversion = 0f;
            Openness = 0f;
        }

        if(Openness > 0.5){
            Conscientiousness_Agreeableness = 0f;
            Neuroticism_Extraversion = 0f;
        }
    }

}
