using UnityEngine;

public class ComposureMeter : MonoBehaviour
{
    [Header("Composure (0 = total panic, 100 = fully calm)")]
    public float maxComposure = 100f;
    public float currentComposure = 100f;

    [Header("Panic Sources (per second)")]
    public float movingFastPanic = 4f;
    public float hallucinationNearPanic = 8f;
    public float lookingAtHallucinationPanic = 5f;
    public float hidingNearHallucinationPanic = 6f;

    [Header("Recovery (per second)")]
    public float stillnessRecovery = 6f;
    public float calmWalkRecovery = 2f;
    public float groundingRecovery = 12f;
    [Range(0f, 1f)] public float recoveryFactorNearHallucination = 0.15f; // recovery barely works while it's near

    [Header("Thresholds")]
    public float panicThreshold = 30f;

    [Header("State (read/set by other systems)")]
    public bool isPanicking = false;
    public bool isNearHallucination = false;
    public bool isLookingAtHallucination = false;
    public bool isGrounding = false;

    [Header("References")]
    public CharacterController playerCC;
    public PlayerController playerController;
    public Transform cameraTransform;

    [Header("Heartbeat Audio")]
    public AudioSource heartbeatSource;
    public float heartbeatMaxVolume = 1f;

    [Header("Panic Tremble")]
    public float trembleMaxAmount = 0.35f;
    public float trembleFrequency = 22f;
    public float trembleStartBelow = 60f;

    [Header("Collapse Tracking (Layer 3)")]
    public float collapseTimer = 0f;
    public float collapseThreshold = 10f;

    void Start()
    {
        if (playerCC == null) playerCC = GetComponent<CharacterController>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        if (heartbeatSource != null)
        {
            heartbeatSource.loop = true;
            heartbeatSource.volume = 0f;
            heartbeatSource.Play();
        }
    }

    void Update()
    {
        float delta = 0f;
        float speed = playerCC != null ? playerCC.velocity.magnitude : 0f;

        // === Panic sources ===
        if (speed > 2.5f)                  delta -= movingFastPanic;
        if (isNearHallucination)           delta -= hallucinationNearPanic;
        if (isLookingAtHallucination)      delta -= lookingAtHallucinationPanic;
        if (playerController != null && playerController.isCrouching && isNearHallucination)
            delta -= hidingNearHallucinationPanic;

        // === Recovery sources (suppressed while a hallucination is near) ===
        float recovery = 0f;
        if (isGrounding)                   recovery = groundingRecovery;
        else if (speed < 0.1f)             recovery = stillnessRecovery;
        else if (speed < 1.8f)             recovery = calmWalkRecovery;

        if (isNearHallucination)
            recovery *= recoveryFactorNearHallucination;  // you can't calmly regenerate while it stalks you

        delta += recovery;

        currentComposure += delta * Time.deltaTime;
        currentComposure = Mathf.Clamp(currentComposure, 0f, maxComposure);

        isPanicking = currentComposure < panicThreshold;

        // === Layer 3 collapse tracking ===
        if (currentComposure < collapseThreshold)
            collapseTimer += Time.deltaTime;

        // === Heartbeat feedback ===
        if (heartbeatSource != null)
        {
            float panicT = 1f - (currentComposure / maxComposure);
            heartbeatSource.volume = Mathf.Lerp(0f, heartbeatMaxVolume, panicT * panicT);
            heartbeatSource.pitch = Mathf.Lerp(0.85f, 1.4f, panicT);
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;
        if (currentComposure >= trembleStartBelow) return;

        float trembleT = 1f - (currentComposure / trembleStartBelow);
        float amount = trembleMaxAmount * trembleT * trembleT;

        float nx = (Mathf.PerlinNoise(Time.time * trembleFrequency, 0f) - 0.5f) * 2f;
        float ny = (Mathf.PerlinNoise(0f, Time.time * trembleFrequency) - 0.5f) * 2f;

        cameraTransform.localRotation *= Quaternion.Euler(nx * amount, ny * amount * 0.6f, nx * amount * 0.3f);
    }

    public bool BeliefCheck() => isPanicking;

    public void Shock(float amount)
    {
        currentComposure -= amount;
        currentComposure = Mathf.Max(currentComposure, 0f);
    }

    public float GetNormalized() => currentComposure / maxComposure;
}