using TMPro;
using UnityEngine;

/// <summary>
/// GameplayStatsDisplay — displays both current score and money during gameplay.
/// 
/// Setup:
///   1. Attach to a parent container or separate GameObject in your game scene.
///   2. Assign TextMeshProUGUI elements for score and money display.
///   3. Updates every frame to show live stats.
/// </summary>
public class GameplayStatsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private string scoreFormat = "S: {0:D6}";
    [SerializeField] private string moneyFormat = "M: {0:D6}";

    private void Update()
    {
        UpdateScore();
        UpdateMoney();
    }

    private void UpdateScore()
    {
        if (scoreText == null)
        {
            return;
        }

        if (KillScoreManager.Instance != null)
        {
            int currentScore = KillScoreManager.Instance.CurrentScore;
            scoreText.text = string.Format(scoreFormat, currentScore);
        }
    }

    private void UpdateMoney()
    {
        if (moneyText == null)
        {
            return;
        }

        if (RingScoreManager.Instance != null)
        {
            int currentSessionMoney = RingScoreManager.Instance.currentScore;
            moneyText.text = string.Format(moneyFormat, currentSessionMoney);
        }
    }
}
