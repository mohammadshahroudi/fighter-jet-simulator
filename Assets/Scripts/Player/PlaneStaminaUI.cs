using UnityEngine;
using UnityEngine.UI;

public class PlaneStaminaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Image barImage;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.onStaminaChanged.AddListener(HandleStaminaChanged);
        }
    }

    private void Start()
    {
        if (playerController != null && barImage != null)
        {
            barImage.fillAmount = 1f;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.onStaminaChanged.RemoveListener(HandleStaminaChanged);
        }
    }

    private void HandleStaminaChanged(float staminaNormalized)
    {
        if (barImage == null) return;
        barImage.fillAmount = Mathf.Clamp01(staminaNormalized);
    }
}
