using UnityEngine;

[DisallowMultipleComponent]
public class UfoWarpArrivalAudio : MonoBehaviour
{
    [Header("Warp-In SFX")]
    [SerializeField] private AudioClip warpInSfx;
    [SerializeField] [Range(0f, 1f)] private float warpInVolume = 0.55f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool playOnlyOnce = true;
    [SerializeField] private bool use3DAudio = true;

    private AudioSource audioSource;
    private bool hasPlayed;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = use3DAudio ? 1f : 0f;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            PlayWarpIn();
    }

    public void PlayWarpIn()
    {
        if (warpInSfx == null || audioSource == null)
            return;

        if (playOnlyOnce && hasPlayed)
            return;

        audioSource.PlayOneShot(warpInSfx, warpInVolume);
        hasPlayed = true;
    }
}
