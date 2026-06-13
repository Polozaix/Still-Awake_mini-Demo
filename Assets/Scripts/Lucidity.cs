using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class LucidityMeter : MonoBehaviour
{
    [Header("Lucidity Settings")]
    public float maxLucidityCeiling = 100f;   // the permanent ceiling (Layer 2 erosion lowers this)
    public float currentMaxLucidity = 100f;   // current ceiling this run
    public float currentLucidity = 100f;

    [Header("Drain Rates (per second)")]
    public float baseDrainRate = 0.5f;        // daytime / calm drain
    public float nightDrainMultiplier = 2f;   // multiply at night
    public bool isNight = false;              // set this from your day/night system later

    [Header("Post Processing")]
    public Volume globalVolume;

    // HDRP effects
    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;
    private FilmGrain _filmGrain;
    private LensDistortion _lensDistortion;

    // Tiers
    public enum LucidityTier { Clear, Uneasy, Distorted, Breaking }
    public LucidityTier currentTier = LucidityTier.Clear;

    // Events for other systems (hallucination spawner will listen to this)
    public static event System.Action<LucidityTier> OnTierChanged;

    void Start()
    {
        if (globalVolume == null)
            globalVolume = FindAnyObjectByType<Volume>();

        globalVolume.profile.TryGet(out _vignette);
        globalVolume.profile.TryGet(out _chromaticAberration);
        globalVolume.profile.TryGet(out _filmGrain);
        globalVolume.profile.TryGet(out _lensDistortion);
    }

    void Update()
    {
        Drain();
        UpdatePostProcessing();
        CheckTier();
    }

    void Drain()
    {
        float rate = baseDrainRate * (isNight ? nightDrainMultiplier : 1f);
        currentLucidity -= rate * Time.deltaTime;
        currentLucidity = Mathf.Clamp(currentLucidity, 0f, currentMaxLucidity);
    }

    void UpdatePostProcessing()
    {
        // t: 0 = fully lucid, 1 = fully gone
        float t = 1f - (currentLucidity / maxLucidityCeiling);

        if (_vignette != null)
            _vignette.intensity.value = Mathf.Lerp(0.2f, 0.45f, t);

        if (_chromaticAberration != null)
            _chromaticAberration.intensity.value = Mathf.Lerp(0f, 1f, t);

        if (_filmGrain != null)
            _filmGrain.intensity.value = Mathf.Lerp(0f, 1f, t);

        if (_lensDistortion != null)
            _lensDistortion.intensity.value = Mathf.Lerp(0f, -0.4f, t);
    }

    void CheckTier()
    {
        float pct = (currentLucidity / maxLucidityCeiling) * 100f;
        LucidityTier newTier;

        if (pct > 75f)      newTier = LucidityTier.Clear;
        else if (pct > 50f) newTier = LucidityTier.Uneasy;
        else if (pct > 25f) newTier = LucidityTier.Distorted;
        else                newTier = LucidityTier.Breaking;

        if (newTier != currentTier)
        {
            currentTier = newTier;
            OnTierChanged?.Invoke(currentTier);
        }
    }

    // === Public API for other systems ===

    /// Layer 2 erosion: called by the pass-out system. Permanently lowers the ceiling this run.
    public void ErodeCeiling(float amount)
    {
        currentMaxLucidity -= amount;
        currentMaxLucidity = Mathf.Max(currentMaxLucidity, 10f); // never fully zero
        currentLucidity = Mathf.Min(currentLucidity, currentMaxLucidity);
    }

    /// Small recovery (grounding objects, dawn, etc.)
    public void Recover(float amount)
    {
        currentLucidity = Mathf.Min(currentLucidity + amount, currentMaxLucidity);
    }

    public float GetNormalized() => currentLucidity / maxLucidityCeiling;
}