using TMPro;
using UnityEngine;


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
