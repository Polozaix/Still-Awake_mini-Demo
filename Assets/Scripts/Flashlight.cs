using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [Header("References")]
    public Light flashlightLight;       // the spot light
    public Transform cameraTransform;   // the Main Camera

    [Header("Aim Feel")]
    public float followSmoothing = 8f;  // higher = snappier, lower = more lag/sway

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.F;
    public AudioClip toggleOnSound;
    public AudioClip toggleOffSound;

    [Header("Flicker (optional)")]
    public bool enableSubtleFlicker = false;
    public float baseIntensity = 15f;

    private bool _isOn = true;
    private AudioSource _audioSource;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        _audioSource = GetComponent<AudioSource>();
        if (flashlightLight != null)
            baseIntensity = flashlightLight.intensity;
    }

    void Update()
    {
        HandleToggle();
        HandleAim();
        if (enableSubtleFlicker && _isOn) HandleFlicker();
    }

    void HandleAim()
    {
        if (cameraTransform == null) return;

        // smoothly match the camera's aim direction — slight lag feels like a held torch
        Quaternion targetRot = cameraTransform.rotation;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            followSmoothing * Time.deltaTime
        );
    }

    void HandleToggle()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _isOn = !_isOn;
            if (flashlightLight != null)
                flashlightLight.enabled = _isOn;

            if (_audioSource != null)
            {
                AudioClip clip = _isOn ? toggleOnSound : toggleOffSound;
                if (clip != null) _audioSource.PlayOneShot(clip);
            }
        }
    }

    void HandleFlicker()
    {
        // subtle organic flicker
        float flicker = Mathf.PerlinNoise(Time.time * 8f, 0f);
        flashlightLight.intensity = baseIntensity * Mathf.Lerp(0.92f, 1f, flicker);
    }
}
