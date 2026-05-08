using UnityEngine;
using TMPro;

public class PlayerMoneyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;

    private void Start()
    {
        UpdateMoneyDisplay();
    }

    public void UpdateMoneyDisplay()
    {
        int currentMoney = ShopPersistence.LoadMoney();
        if (moneyText != null)
            moneyText.text = $"$ {currentMoney:000000}";
    }
}