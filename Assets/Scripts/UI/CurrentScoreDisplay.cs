using TMPro;
using UnityEngine;

/// <summary>
/// CurrentScoreDisplay — displays the current kill score during gameplay.
/// 
/// Setup:
///   1. Attach to a TextMeshProUGUI element in your game scene.
///   2. It will automatically read and display the current score from KillScoreManager.
///   3. Updates every frame to show live score changes.
/// </summary>
public class CurrentScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string displayFormat = "S: {0:D6}";

    private void Update()
    {
        if (scoreText == null)
        {
            Debug.LogWarning("[CurrentScoreDisplay] TextMeshProUGUI reference is missing!");
            return;
        }

        if (KillScoreManager.Instance != null)
        {
            int currentScore = KillScoreManager.Instance.CurrentScore;
            scoreText.text = string.Format(displayFormat, currentScore);
        }
    }
}
