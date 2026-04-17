using UnityEngine;
using TMPro;

public class ShopUIController : MonoBehaviour
{
    public PlayerUpgradeData data;
    public ShopPurchaseManager shopPurchaseManager;

    [Header("UI")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI speedButtonText;
    public TextMeshProUGUI healthButtonText;
    public TextMeshProUGUI damageButtonText;
    public TextMeshProUGUI bulletTypeButtonText;
    public TextMeshProUGUI missilesButtonText;

    private void OnEnable()
    {
        if (shopPurchaseManager != null)
        {
            shopPurchaseManager.OnShopChanged += RefreshUI;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (shopPurchaseManager != null)
        {
            shopPurchaseManager.OnShopChanged -= RefreshUI;
        }
    }

    private void OnValidate()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (data == null)
        {
            return;
        }

        if (moneyText != null)
        {
            moneyText.text = "$" + data.points;
        }

        if (speedButtonText != null)
        {
            speedButtonText.text = data.speedLevel >= data.maxSpeedLevel
                ? "Speed\nMAX"
                : "Speed\n$" + data.GetSpeedCost();
        }

        if (healthButtonText != null)
        {
            healthButtonText.text = data.healthLevel >= data.maxHealthLevel
                ? "Health\nMAX"
                : "Health\n$" + data.GetHealthCost();
        }

        if (damageButtonText != null)
        {
            damageButtonText.text = data.damageLevel >= data.maxDamageLevel
                ? "Damage\nMAX"
                : "Damage\n$" + data.GetDamageCost();
        }

        if (bulletTypeButtonText != null)
        {
            bulletTypeButtonText.text = data.bulletType == "Advanced"
                ? "Bullet\nUNLOCKED"
                : "Bullet\n$" + data.bulletTypeUpgradeCost;
        }

        if (missilesButtonText != null)
        {
            missilesButtonText.text = data.missilesUnlocked
                ? "Missiles\nUNLOCKED"
                : "Missiles\n$" + data.missileUnlockCost;
        }
    }

}