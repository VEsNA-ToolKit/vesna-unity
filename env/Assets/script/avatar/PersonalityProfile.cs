using UnityEngine;

[System.Serializable]
public class PersonalityProfile
{
    [Range(0f, 1f)] public float Estroversione;
    // [Range(0f, 1f)] public float Introversione;
    [Range(0f, 1f)] public float Gradevolezza;
    // [Range(0f, 1f)] public float Nevroticismo;
    [Range(0f, 1f)] public float Coscienziosità;
    // [Range(0f, 1f)] public float AperturaAlleEsperienze;

    public void Validate()
    {
        /*float total = Estroversione + Introversione + 
            Gradevolezza + Nevroticismo + Coscienziosità + AperturaAlleEsperienze;*/
        
        float total = Estroversione + 
            Gradevolezza + Coscienziosità;

        Debug.Log("Total: " + total);
        
        if(total > 1f){
            DefaultValues();
        }
    }

    public void DefaultValues(){ 
        Estroversione = 0f;
        //Introversione = 0f;
        Gradevolezza = 0f;
        //Nevroticismo = 0f;
        Coscienziosità = 0f;
        //AperturaAlleEsperienze = 0f;
    }

}
