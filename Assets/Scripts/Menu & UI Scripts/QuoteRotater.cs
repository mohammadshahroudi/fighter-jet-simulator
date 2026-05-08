using TMPro;
using UnityEngine;

public class QuoteRotater : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI quoteText; 
    
    [Header("Text Lines")]
    [TextArea(3, 10)] 
    [SerializeField] private string[] lines;
    
    void Start()
    {
        quoteText.text = lines[Random.Range(0, lines.Length)]; 
        // quoteText.text = lines[18];
    }
}
