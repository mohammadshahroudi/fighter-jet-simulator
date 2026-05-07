using TMPro;
using UnityEngine;

/// <summary>
/// HighScoreDisplay — displays the saved high score on the main menu.
/// 
/// Setup:
///   1. Attach to a TextMeshProUGUI element on your main menu.
///   2. It will automatically load and display the saved high score from PlayerPrefs.
/// </summary>
public class HighScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private string displayFormat = "HS: {0:D6}";

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (highScoreText == null)
        {
            Debug.LogWarning("[HighScoreDisplay] TextMeshProUGUI reference is missing!");
            return;
        }

        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = string.Format(displayFormat, highScore);
    }
}
