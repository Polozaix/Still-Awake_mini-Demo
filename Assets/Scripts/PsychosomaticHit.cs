using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

public class PsychosomaticHit : MonoBehaviour
{
    [Header("Hit Effects")]
    public float hitDuration = 2.5f;          // how long one hit's effects last
    public float staggerStrength = 3f;        // camera kick
    public float tunnelVignetteMax = 0.65f;   // vignette spike during hit

    [Header("Layer 1 — Pass-Out Tracking")]
    public int hitsToPassOut = 3;             // hits within the window = pass out
    public float hitWindowSeconds = 60f;      // window the hits must fall within
    public float passOutDurationSeconds = 4f; // real-time blackout length
    public float lucidityErosionPerPassOut = 10f; // Layer 2: ceiling drop

    [Header("Audio")]
    public AudioSource heartbeatSource;       // same one ComposureMeter uses
    public AudioClip hitSting;                // sharp sting on impact

    [Header("References (auto-found if empty)")]
    public Volume globalVolume;
    public LucidityMeter lucidity;
    public PlayerController playerController;

    private Vignette _vignette;
    private AudioSource _stingSource;
    private float[] _recentHits;
    private int _hitIndex = 0;
    private bool _inHit = false;
    private bool _passedOut = false;
    private CanvasGroup _blackoutCanvas;      // optional: assign a full-black UI canvas

    [Header("Optional Blackout UI")]
    public CanvasGroup blackoutCanvasGroup;   // full-screen black image with CanvasGroup

    void Start()
    {
        if (globalVolume == null) globalVolume = FindAnyObjectByType<Volume>();
        if (lucidity == null) lucidity = FindAnyObjectByType<LucidityMeter>();
        if (playerController == null) playerController = GetComponent<PlayerController>();

        globalVolume.profile.TryGet(out _vignette);
        _stingSource = gameObject.AddComponent<AudioSource>();
        _stingSource.playOnAwake = false;

        _recentHits = new float[hitsToPassOut];
        for (int i = 0; i < _recentHits.Length; i++) _recentHits[i] = -9999f;
    }

    /// Called by Hallucination.ResolveContact() when the belief check fails
    public void TriggerHit()
    {
        if (_passedOut) return;

        // record hit time
        _recentHits[_hitIndex] = Time.time;
        _hitIndex = (_hitIndex + 1) % _recentHits.Length;

        // check pass-out: are ALL recorded hits within the window?
        bool allRecent = true;
        foreach (float t in _recentHits)
            if (Time.time - t > hitWindowSeconds) { allRecent = false; break; }

        if (allRecent)
        {
            StartCoroutine(PassOut());
        }
        else if (!_inHit)
        {
            StartCoroutine(HitSequence());
        }
    }

    IEnumerator HitSequence()
    {
        _inHit = true;

        // sting + heartbeat spike
        if (hitSting != null) _stingSource.PlayOneShot(hitSting);
        if (heartbeatSource != null) heartbeatSource.pitch = 1.6f;

        float elapsed = 0f;
        float startVignette = _vignette != null ? _vignette.intensity.value : 0.2f;

        while (elapsed < hitDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hitDuration;

            // tunnel vision: spike fast, recover slow
            if (_vignette != null)
            {
                float spike = t < 0.15f ? Mathf.Lerp(startVignette, tunnelVignetteMax, t / 0.15f)
                                        : Mathf.Lerp(tunnelVignetteMax, startVignette, (t - 0.15f) / 0.85f);
                _vignette.intensity.value = spike;
            }

            // camera stagger: sharp kick that decays
            if (Camera.main != null && t < 0.3f)
            {
                float kick = staggerStrength * (1f - t / 0.3f);
                Camera.main.transform.localRotation *= Quaternion.Euler(
                    Random.Range(-kick, kick) * 0.3f,
                    0f,
                    Random.Range(-kick, kick)
                );
            }

            yield return null;
        }

        _inHit = false;
    }

    IEnumerator PassOut()
    {
        _passedOut = true;
        Debug.Log("PASS OUT — Layer 1 fail. Eroding lucidity ceiling (Layer 2).");

        // Layer 2: permanent erosion
        if (lucidity != null) lucidity.ErodeCeiling(lucidityErosionPerPassOut);

        // fade to black
        if (blackoutCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                blackoutCanvasGroup.alpha = t;
                yield return null;
            }
        }

        // disable control during blackout
        if (playerController != null) playerController.enabled = false;

        yield return new WaitForSeconds(passOutDurationSeconds);

        // wake up: disoriented — low vignette pulse, control returns
        if (playerController != null) playerController.enabled = true;

        if (blackoutCanvasGroup != null)
        {
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime * 0.5f;   // slow wake
                blackoutCanvasGroup.alpha = t;
                yield return null;
            }
        }

        // reset hit history
        for (int i = 0; i < _recentHits.Length; i++) _recentHits[i] = -9999f;
        _passedOut = false;
    }
}