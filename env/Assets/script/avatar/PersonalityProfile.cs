using UnityEngine;

[System.Serializable]
public class PersonalityProfile
{
    [Header("Big Five Traits (0 = Low, 1 = High)")]
    [Range(0f, 1f)] public float Neuroticism;
    [Range(0f, 1f)] public float Extraversion;
    [Range(0f, 1f)] public float Conscientiousness;
    [Range(0f, 1f)] public float Agreeableness;
    [Range(0f, 1f)] public float Openness;

    [Header("Avatar Animation Controller")]
    public AvatarFACS avatarFACS;

   
    // Validates and adjusts trait values based on soft correlations found in meta-analytic data.
    public void Validate()
    {
        ApplySoftCorrelations();
        ClampValues();
        UpdateAvatarExpressions();
    }

    // Applies soft correlation adjustments between traits without enforcing deterministic coupling.
    private void ApplySoftCorrelations()
    {
        //Strong negative correlation
        //Conscientiousness ↔ Neuroticism (ρ ≈ -0.43)
        if (Conscientiousness > 0.7f && Neuroticism > 0.7f)
        {
            // If C is very high and N is very high, slightly reduce N
            Neuroticism -= 0.10f * (Conscientiousness - 0.7f);
        }

        if (Neuroticism > 0.7f && Conscientiousness > 0.7f)
        {
            // If N is very high and C is very high, slightly reduce C
            Conscientiousness -= 0.10f * (Neuroticism - 0.7f);
        }

        // Strong positive correlation
        // Openness ↔ Extraversion (ρ ≈ +0.43)
        if (Extraversion > 0.7f && Openness < 1.0f)
        {
            // If E is very high, slightly raise O 
            Openness += 0.08f * (Extraversion - 0.7f);
        }
        if (Openness > 0.7f && Extraversion < 1.0f)
        {
            // If O is very high, slightly raise E  
            Extraversion += 0.08f * (Openness - 0.7f);
        }

        // Conscientiousness ↔ Agreeableness (ρ ≈ +0.43)
        if (Conscientiousness > 0.7f && Agreeableness < 1.0f)
        {
            // If C is very high, slightly raise A 
            Agreeableness += 0.08f * (Conscientiousness - 0.7f);
        }
        if (Agreeableness > 0.7f && Conscientiousness < 1.0f)
        {
            // If A is very high, slightly raise C 
            Conscientiousness += 0.08f * (Agreeableness - 0.7f);
        }

    }

    // Ensures all trait values remain within the [0, 1] range.
    private void ClampValues()
    {
        Neuroticism = Mathf.Clamp01(Neuroticism);
        Extraversion = Mathf.Clamp01(Extraversion);
        Conscientiousness = Mathf.Clamp01(Conscientiousness);
        Agreeableness = Mathf.Clamp01(Agreeableness);
        Openness = Mathf.Clamp01(Openness);
    }

    // Updates avatar facial expressions based on dominant personality traits.
    private void UpdateAvatarExpressions()
    {
        if (avatarFACS == null) return;

        // Emotional tone / activation
        if (Extraversion >= 0.6f)
            avatarFACS.SetExtraversion();
        else if (Neuroticism >= 0.6f)
            avatarFACS.SetNeuroticism();

        // Social orientation
        if (Agreeableness >= 0.6f)
            avatarFACS.SetAgreeableness();

        // Self-regulation
        if (Conscientiousness >= 0.6f)
            avatarFACS.SetConscientiousness();

        // Cognitive openness
        if (Openness >= 0.6f)
            avatarFACS.SetHighOpenness();
        else
            avatarFACS.SetLowOpenness();
    }

    // Resets all traits to neutral defaults (midpoint = 0.5).
    public void DefaultValues()
    {
        Neuroticism = 0.5f;
        Extraversion = 0.5f;
        Conscientiousness = 0.5f;
        Agreeableness = 0.5f;
        Openness = 0.5f;
    }
}
