using TMPro;
using UnityEngine;

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
