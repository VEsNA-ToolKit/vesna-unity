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
        // Negative correlation: Extraversion ↔ Neuroticism (r = -0.28)
        if (Extraversion > 0.7f && Neuroticism > 0.7f)
        {
            // Slightly reduce neuroticism for very high extraversion
            Neuroticism -= 0.1f * (Extraversion - 0.7f);
             // Slightly reduce extraversion for very high neuroticism
            Extraversion -= 0.1f * (Neuroticism - 0.7f);
        }

        // Positive correlation: Extraversion ↔ Openness (r = +0.26)
        if (Extraversion > 0.6f)
        {
            Openness += 0.05f * (Extraversion - 0.6f);
        }

        // Negative correlation: Neuroticism ↔ Agreeableness (r = -0.22)
        if (Neuroticism > 0.6f)
        {
            Agreeableness -= 0.05f * (Neuroticism - 0.6f);
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
