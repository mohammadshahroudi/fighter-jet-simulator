using TMPro;
using UnityEngine;

/// <summary>
/// CurrentMoneyDisplay — displays the current money currency from ring collection during gameplay.
/// 
/// Setup:
///   1. Attach to a TextMeshProUGUI element in your game scene.
///   2. It will automatically read and display the current money from PlayerPrefs.
///   3. Updates every frame to show live currency changes.
/// </summary>
public class CurrentMoneyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private string displayFormat = "$: {0:D6}";

    private void Update()
    {
        if (moneyText == null)
        {
            Debug.LogWarning("[CurrentMoneyDisplay] TextMeshProUGUI reference is missing!");
            return;
        }

        if (RingScoreManager.Instance != null)
        {
            int currentSessionMoney = RingScoreManager.Instance.currentScore;
            moneyText.text = string.Format(displayFormat, currentSessionMoney);
        }
    }
}
