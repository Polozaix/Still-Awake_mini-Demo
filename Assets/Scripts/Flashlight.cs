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

    [Header("Walk Sway")]
    public float swayAmount = 0.8f;        // degrees of rotational sway
    public float swayFrequency = 1.4f;     // match your bobFrequency for sync, offset slightly for realism
    public Transform playerBody;            // assign the Player root (to read movement)

    private CharacterController _playerCC;
    private float _swayTimer;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        _audioSource = GetComponent<AudioSource>();
        if (flashlightLight != null)
            baseIntensity = flashlightLight.intensity;
        
        if (playerBody != null)
        _playerCC = playerBody.GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleToggle();
        if (enableSubtleFlicker && _isOn) HandleFlicker();
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

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        transform.position = cameraTransform.position + cameraTransform.TransformDirection(new Vector3(0f, 0f, 0.2f));

        // base target: camera rotation
        Quaternion targetRot = cameraTransform.rotation;

        // add walking sway
        bool isMoving = _playerCC != null && _playerCC.velocity.magnitude > 0.1f;
        if (isMoving)
        {
            _swayTimer += Time.deltaTime * swayFrequency;
            float swayX = Mathf.Sin(_swayTimer * Mathf.PI * 2f) * swayAmount;
            float swayY = Mathf.Sin(_swayTimer * Mathf.PI) * swayAmount * 0.6f;
            targetRot *= Quaternion.Euler(swayY, swayX, 0f);
        }
        else
        {
            _swayTimer = 0f;
        }

         transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            followSmoothing * Time.deltaTime
        );
    }
}
