using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 1.3f;
    public float acceleration = 8f;

    [Header("Sprint & Crouch")]
    public float sprintSpeed = 3.5f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float crouchSpeed = 0.7f;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = 1.0f;
    public float standHeight = 2.0f;
    public float crouchTransitionSpeed = 8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    public float lookSmoothing = 5f;

    [Header("Head Bob")]
    public float bobFrequency = 0.8f;
    public float bobAmplitude = 0.04f;
    public float bobSwayAmount = 0.03f;
    public float sprintBobMultiplier = 1.6f;

    [Header("Breathing")]
    public float breatheFrequency = 0.8f;
    public float breatheAmplitude = 0.004f;

    [Header("Leaning")]
    public float leanDistance = 0.4f;
    public float leanSpeed = 6f;
    public float leanRollAmount = 6f;

    [Header("Footsteps")]
    public AudioClip[] footstepSounds;

    // === State (read by other systems like ComposureMeter) ===
    [HideInInspector] public bool isSprinting = false;
    [HideInInspector] public bool isCrouching = false;

    // private refs
    private CharacterController _cc;
    private Camera _cam;
    private AudioSource _audioSource;

    // movement
    private Vector3 _velocity;
    private float _currentHeight;

    // look
    private float _verticalLook;
    private float _smoothMouseX;
    private float _smoothMouseY;

    // bob
    private float _bobTimer;
    private float _breatheTimer;
    private Vector3 _camDefaultPos;
    private float _lastSine;

    // lean
    private float _currentLean;
    private float _targetLean;
    private float _currentRoll;
    private float _targetRoll;

    void Start()
    {
        _cc = GetComponent<CharacterController>();
        _cam = GetComponentInChildren<Camera>();
        _audioSource = GetComponent<AudioSource>();
        _camDefaultPos = _cam.transform.localPosition;
        _currentHeight = standHeight;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleCameraEffects();
        HandleLean();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // crouch is held; sprint only forward and not while crouched
        isCrouching = Input.GetKey(crouchKey);
        isSprinting = Input.GetKey(sprintKey) && !isCrouching && v > 0;

        float targetSpeed = walkSpeed;
        if (isSprinting) targetSpeed = sprintSpeed;
        if (isCrouching) targetSpeed = crouchSpeed;

        Vector3 input = transform.right * h + transform.forward * v;
        if (input.magnitude > 1f) input.Normalize();

        Vector3 targetVelocity = input * targetSpeed;
        _velocity = Vector3.Lerp(_velocity, targetVelocity, acceleration * Time.deltaTime);

        Vector3 move = _velocity * Time.deltaTime;
        move.y = -9.81f * Time.deltaTime;
        _cc.Move(move);

        // crouch height transition
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        _cc.height = _currentHeight;
        _cc.center = new Vector3(0f, _currentHeight / 2f, 0f);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _smoothMouseX = Mathf.Lerp(_smoothMouseX, mouseX, lookSmoothing * Time.deltaTime);
        _smoothMouseY = Mathf.Lerp(_smoothMouseY, mouseY, lookSmoothing * Time.deltaTime);

        transform.Rotate(Vector3.up * _smoothMouseX);

        _verticalLook -= _smoothMouseY;
        _verticalLook = Mathf.Clamp(_verticalLook, -maxLookAngle, maxLookAngle);

        _cam.transform.localRotation = Quaternion.Euler(_verticalLook, 0f, _currentRoll);
    }

    void HandleCameraEffects()
    {
        // camera eye height follows crouch (eyes near top of capsule)
        _camDefaultPos.y = _currentHeight * 0.85f;

        bool isMoving = _velocity.magnitude > 0.1f;

        if (isMoving)
        {
            float freq = bobFrequency * (isSprinting ? sprintBobMultiplier : 1f);
            _bobTimer += Time.deltaTime * freq;

            float sine = Mathf.Sin(_bobTimer * Mathf.PI * 2f);
            float swayX = sine * bobSwayAmount;
            float dip = Mathf.Abs(sine) * -bobAmplitude;

            Vector3 targetBob = _camDefaultPos + new Vector3(swayX, dip, 0f);
            _cam.transform.localPosition = Vector3.Lerp(
                _cam.transform.localPosition,
                targetBob,
                10f * Time.deltaTime
            );

            // footstep on every dip and peak (both zero crossings)
            if ((_lastSine >= 0f && sine < 0f) || (_lastSine <= 0f && sine > 0f))
            {
                PlayFootstep();
            }
            _lastSine = sine;
        }
        else
        {
            _bobTimer = 0f;
            _lastSine = 0f;
            _breatheTimer += Time.deltaTime * breatheFrequency;
            float breatheY = Mathf.Sin(_breatheTimer * Mathf.PI * 2f) * breatheAmplitude;
            _cam.transform.localPosition = Vector3.Lerp(
                _cam.transform.localPosition,
                _camDefaultPos + new Vector3(0f, breatheY, 0f),
                5f * Time.deltaTime
            );
        }
    }

    void HandleLean()
    {
        _targetLean = 0f;
        _targetRoll = 0f;

        if (Input.GetKey(KeyCode.Q))
        {
            _targetLean = -leanDistance;
            _targetRoll = leanRollAmount;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            _targetLean = leanDistance;
            _targetRoll = -leanRollAmount;
        }

        _currentLean = Mathf.Lerp(_currentLean, _targetLean, leanSpeed * Time.deltaTime);
        _currentRoll = Mathf.Lerp(_currentRoll, _targetRoll, leanSpeed * Time.deltaTime);

        _cam.transform.localPosition = new Vector3(
            Mathf.Lerp(_cam.transform.localPosition.x, _camDefaultPos.x + _currentLean, leanSpeed * Time.deltaTime),
            _cam.transform.localPosition.y,
            _cam.transform.localPosition.z
        );
    }

    void PlayFootstep()
    {
        if (footstepSounds.Length == 0 || _audioSource == null) return;
        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        // crouched steps are quieter, sprint steps louder
        float vol = isCrouching ? Random.Range(0.35f, 0.5f)
                  : isSprinting ? Random.Range(0.9f, 1f)
                  : Random.Range(0.8f, 1f);
        _audioSource.PlayOneShot(clip, vol);
    }
}