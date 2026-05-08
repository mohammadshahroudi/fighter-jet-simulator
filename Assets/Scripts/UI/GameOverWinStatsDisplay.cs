using TMPro;
using UnityEngine;

public class GameOverWinStatsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;

    public void Refresh()
    {
        if (statsText == null)
        {
            return;
        }

        int currentScore = KillScoreManager.Instance != null ? KillScoreManager.Instance.CurrentScore : 0;
        int highScore = KillScoreManager.Instance != null ? KillScoreManager.Instance.HighScore : 0;
        int moneyEarned = RingScoreManager.Instance != null ? RingScoreManager.Instance.currentScore : 0;
        int totalMoney = ShopPersistence.LoadMoney();

        Debug.Log($"[GameOverWinStatsDisplay] current={currentScore} high={highScore} earned={moneyEarned} total={totalMoney}");

        string stats = string.Format("SCORE: {0:D6}\n" +
                         "HI-SCORE: {1:D6}\n" +
                         "MONEY EARNED: {2:D6}\n" +
                         "TOTAL MONEY: {3:D6}", currentScore, highScore, moneyEarned, totalMoney);

        statsText.text = stats;
    }
}
