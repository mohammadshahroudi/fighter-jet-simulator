using UnityEngine;
using UnityEngine.UI;

public class PlaneHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Image barImage;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }
    }
    
    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.onHealthChanged.AddListener(HandleHealthChanged);
        }
    }

    private void Start()
    {
        if (playerStats != null)
        {
            HandleHealthChanged(playerStats.CurrentHealth);
        }
        else if (barImage != null)
        {
            barImage.fillAmount = 0f;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.onHealthChanged.RemoveListener(HandleHealthChanged);
        }
    }

    private void HandleHealthChanged(int currentHealth)
    {
        if (playerStats == null || barImage == null)
        {
            return;
        }

        float healthFraction = playerStats.MaxHealth > 0
            ? (float)currentHealth / playerStats.MaxHealth
            : 0f;

        barImage.fillAmount = Mathf.Clamp01(healthFraction);
    }
}
