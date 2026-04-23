using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RingScoreManager — listens to Ring.OnRingCollected and keeps a running score.
///
/// Setup:
///   1. Attach to any persistent GameObject (e.g. GameManager).
///   2. Optionally assign a TextMeshProUGUI or legacy Text component for on-screen display.
///   3. Optionally assign a scorePopupPrefab (world-space TextMesh) for floating +pts text.
/// </summary>
public class RingScoreManager : MonoBehaviour
{
    [Header("Score")]
    public int currentScore { get; private set; } = 0;

    [Header("UI")]
    [Tooltip("TextMeshPro label to display the score. Optional.")]
    public TMP_Text scoreTMP;

    [Tooltip("Legacy UI Text label. Used only if scoreTMP is null.")]
    public Text scoreLegacyText;

    [Tooltip("Format string for the score display. {0} = score value.")]
    public string scoreFormat = "SCORE: {0}";

    [Header("Floating Popup")]
    [Tooltip("Optional prefab with a TextMesh or TMP component for floating +100 text.")]
    public GameObject scorePopupPrefab;

    [Tooltip("How high above the collection point the popup floats.")]
    public float popupHeight = 5f;
  
    // -------------------------------------------------------------------------
    // Subscribe / unsubscribe safely
    // -------------------------------------------------------------------------

    void OnEnable()  => Ring.OnRingCollected += HandleRingCollected;
    void OnDisable() => Ring.OnRingCollected -= HandleRingCollected;

    void Start() => UpdateScoreUI();

    // -------------------------------------------------------------------------
    // Handler
    // -------------------------------------------------------------------------

    void HandleRingCollected(int points, Vector3 worldPos)
    {
        currentScore += points;
        UpdateScoreUI();
        SpawnPopup(points, worldPos);

        // Save money to PlayerPrefs
        int currentMoney = ShopPersistence.LoadMoney();
        int newMoney = currentMoney + points;
        Debug.Log($"[RingScoreManager] Collected ring: +{points}, previous money: {currentMoney}, new money: {newMoney}");
        ShopPersistence.SaveRuntimeState(newMoney, /*selectedPlaneIndex*/ 0, null); // Use correct selectedPlaneIndex and planes if available
        Debug.Log($"[RingScoreManager] Money saved to PlayerPrefs: {newMoney}");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    void UpdateScoreUI()
    {
        string text = string.Format(scoreFormat, currentScore);

        if (scoreTMP != null)
            scoreTMP.text = text;
        else if (scoreLegacyText != null)
            scoreLegacyText.text = text;
    }

    void SpawnPopup(int points, Vector3 pos)
    {
        if (scorePopupPrefab == null) return;

        Vector3 spawnPos = pos + Vector3.up * popupHeight;
        var popup = Instantiate(scorePopupPrefab, spawnPos, Quaternion.identity);

        // Try to set the text on the popup
        var tmp = popup.GetComponentInChildren<TMP_Text>();
        if (tmp != null) { tmp.text = $"+{points}"; return; }

        var tm = popup.GetComponentInChildren<TextMesh>();
        if (tm != null) tm.text = $"+{points}";
    }

    /// <summary>Reset score to zero (call on new game / level restart).</summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }
}
