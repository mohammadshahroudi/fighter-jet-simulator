using UnityEngine;
using TMPro;

public class TextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI altimeterText;
    [SerializeField] private PlayerController player;
    
    private void Update()
    {
        altimeterText.text = $"Altitude:\n{(int) player.gameObject.transform.position.y}\nfeet";
        speedText.text = $"Speed:\n{(int) Vector3.Dot(player.gameObject.GetComponent<Rigidbody>().linearVelocity, player.gameObject.transform.forward) }\nkts";
        // headingText = $"Heading:\n{(int) player.gameObject.transform.localRotation.y}";
    }
}
