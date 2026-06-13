using UnityEngine;

public class Hallucination : MonoBehaviour
{
    [Header("Behaviour")]
    public float approachSpeed = 0.9f;       // faster — it arrives before you're ready
    public float nearDistance = 5f;
    public float contactDistance = 1.2f;
    public float lookAngle = 25f;

    [Header("Calm-Fade (belief starvation)")]
    public float fadeWhenCalmRate = 0.18f;   // ~6s of held nerve to dispel at full lucidity
    public float fadeMaxDistance = 2.5f;     // must let it get CLOSE to face it down
    public float strengthRegenRate = 0.25f;
    [Range(0f, 1f)] public float strength = 1f;

    [Header("Proximity Pressure")]
    public float pressureMaxDistance = 8f;
    public float maxPressurePerSecond = 16f; // outpaces suppressed recovery — the standoff is a losing battle

    [Header("References (auto-found if empty)")]
    public Transform player;
    public ComposureMeter composure;
    public LucidityMeter lucidity;

    private Renderer _renderer;
    private bool _contactResolved = false;

    void Start()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (composure == null) composure = FindAnyObjectByType<ComposureMeter>();
        if (lucidity == null) lucidity = FindAnyObjectByType<LucidityMeter>();
        _renderer = GetComponentInChildren<Renderer>();
    }

    void Update()
    {
        if (player == null || composure == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool playerLooking = IsPlayerLookingAtMe();

        float lucidityT = lucidity != null ? (1f - lucidity.GetNormalized()) : 0.5f;
        float effectiveSpeed = approachSpeed * Mathf.Lerp(0.8f, 2f, lucidityT);
        float effectiveFade = fadeWhenCalmRate * Mathf.Lerp(1.5f, 0.4f, lucidityT);

        composure.isNearHallucination = dist <= nearDistance;
        composure.isLookingAtHallucination = playerLooking && dist <= nearDistance * 2f;

        if (dist < pressureMaxDistance)
        {
            float proximityT = 1f - (dist / pressureMaxDistance);
            composure.Shock(maxPressurePerSecond * proximityT * proximityT * Time.deltaTime);
        }

        if (dist > contactDistance && strength > 0.05f)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0f;
            transform.position += dir * effectiveSpeed * Time.deltaTime;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        if (playerLooking && !composure.isPanicking && dist <= fadeMaxDistance)
        {
            strength -= effectiveFade * Time.deltaTime;
        }
        else if (composure.isPanicking)
        {
            strength += strengthRegenRate * Time.deltaTime;
        }
        strength = Mathf.Clamp01(strength);

        UpdateVisual();

        if (dist <= contactDistance && !_contactResolved)
        {
            ResolveContact();
        }

        if (strength <= 0.02f)
        {
            Dissolve();
        }
    }

    bool IsPlayerLookingAtMe()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 toMe = (transform.position - cam.transform.position).normalized;
        float angle = Vector3.Angle(cam.transform.forward, toMe);
        return angle < lookAngle;
    }

    void ResolveContact()
    {
        _contactResolved = true;

        bool itHurts = composure.BeliefCheck();

        if (itHurts)
        {
            composure.Shock(25f);
            PsychosomaticHit hitSystem = player.GetComponent<PsychosomaticHit>();
            if (hitSystem != null) hitSystem.TriggerHit();
            Debug.Log("PSYCHOSOMATIC HIT \u2014 the player believed.");
        }
        else
        {
            Debug.Log("PASS-THROUGH \u2014 the player did not believe. It was never there.");
        }

        Dissolve();
    }

    void Dissolve()
    {
        composure.isNearHallucination = false;
        composure.isLookingAtHallucination = false;
        Destroy(gameObject);
    }

    void UpdateVisual()
    {
        if (_renderer == null) return;
        Color c = _renderer.material.color;
        c.a = strength;
        _renderer.material.color = c;
    }
}