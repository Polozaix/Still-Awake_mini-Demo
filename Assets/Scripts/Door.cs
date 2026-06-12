using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Settings")]
    public float openAngle = 90f;
    public float speed = 3f;
    public KeyCode interactKey = KeyCode.F;
    public float interactDistance = 2.5f;

    [Header("Audio (optional)")]
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool _isOpen = false;
    private Quaternion _closedRot;
    private Quaternion _openRot;
    private Transform _player;
    private AudioSource _audio;

    void Start()
    {
        _closedRot = transform.rotation;
        _openRot = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
        _player = Camera.main.transform;
        _audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        // check interact
        if (Input.GetKeyDown(interactKey))
        {
            float dist = Vector3.Distance(_player.position, transform.position);
            if (dist <= interactDistance)
            {
                _isOpen = !_isOpen;
                if (_audio != null)
                {
                    AudioClip clip = _isOpen ? openSound : closeSound;
                    if (clip != null) _audio.PlayOneShot(clip);
                }
            }
        }

        // smooth swing
        Quaternion target = _isOpen ? _openRot : _closedRot;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, speed * Time.deltaTime);
    }
}
