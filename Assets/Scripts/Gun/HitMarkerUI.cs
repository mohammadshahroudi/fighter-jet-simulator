using UnityEngine;
using UnityEngine.UI;

public class HitMarkerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunLogic gunLogic;
    [SerializeField] private Image hitMarkerImage;
    [SerializeField] private AudioSource hitAudioSource;

    [Header("Hit Marker Appearance")]
    [SerializeField] private Sprite hitMarkerSprite;
    [SerializeField] private Color hitMarkerColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float hitMarkerSize = 50f;
    [SerializeField] private float hitMarkerDuration = 0.15f;

    [Header("Animation")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float scaleStartMultiplier = 1.5f;
    [SerializeField] private float scaleEndMultiplier = 1.0f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [SerializeField] private bool useFadeAnimation = true;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] [Range(0f, 1f)] private float hitSoundVolume = 0.5f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Positioning")]
    [SerializeField] private bool centerOnScreen = true;
    [SerializeField] private Vector2 screenOffset = Vector2.zero;

    private float hitMarkerTimer = 0f;
    private bool isDisplaying = false;
    private Vector3 baseScale;

    void Start()
    {
        if (gunLogic == null)
        {
            gunLogic = FindObjectOfType<GunLogic>();
        }

        if (gunLogic != null)
        {
            gunLogic.OnTargetHit += ShowHitMarker;
        }

        if (hitMarkerImage != null)
        {
            hitMarkerImage.enabled = false;
            baseScale = hitMarkerImage.transform.localScale;

            if (hitMarkerSprite != null)
            {
                hitMarkerImage.sprite = hitMarkerSprite;
            }

            RectTransform rectTransform = hitMarkerImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(hitMarkerSize, hitMarkerSize);
            }
        }

        if (hitAudioSource == null)
        {
            hitAudioSource = gameObject.AddComponent<AudioSource>();
            hitAudioSource.playOnAwake = false;
            hitAudioSource.spatialBlend = 0f; // 2D sound
        }
    }

    void OnDestroy()
    {
        if (gunLogic != null)
        {
            gunLogic.OnTargetHit -= ShowHitMarker;
        }
    }

    void Update()
    {
        if (isDisplaying)
        {
            hitMarkerTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(hitMarkerTimer / hitMarkerDuration);

            if (hitMarkerImage != null)
            {
                // Scale animation
                if (useScaleAnimation)
                {
                    float scaleValue = Mathf.Lerp(scaleStartMultiplier, scaleEndMultiplier, scaleCurve.Evaluate(progress));
                    hitMarkerImage.transform.localScale = baseScale * scaleValue;
                }

                // Fade animation
                if (useFadeAnimation)
                {
                    Color color = hitMarkerColor;
                    color.a *= fadeCurve.Evaluate(progress);
                    hitMarkerImage.color = color;
                }
            }

            // Hide after duration
            if (progress >= 1f)
            {
                HideHitMarker();
            }
        }
    }

    void ShowHitMarker(Vector3 hitWorldPosition)
    {
        if (hitMarkerImage == null) return;

        // Reset timer
        hitMarkerTimer = 0f;
        isDisplaying = true;

        // Show image
        hitMarkerImage.enabled = true;
        hitMarkerImage.color = hitMarkerColor;
        hitMarkerImage.transform.localScale = baseScale * scaleStartMultiplier;

        // Position on screen
        if (centerOnScreen)
        {
            RectTransform rectTransform = hitMarkerImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = screenOffset;
            }
        }

        // Play sound
        PlayHitSound();
    }

    void HideHitMarker()
    {
        isDisplaying = false;
        if (hitMarkerImage != null)
        {
            hitMarkerImage.enabled = false;
        }
    }

    void PlayHitSound()
    {
        if (hitAudioSource == null || hitSound == null) return;

        if (randomizePitch)
        {
            hitAudioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            hitAudioSource.pitch = 1f;
        }

        hitAudioSource.PlayOneShot(hitSound, hitSoundVolume);
    }
}
