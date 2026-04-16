using System;
using UnityEngine;

public class ShopPurchaseManager : MonoBehaviour
{
    public PlayerUpgradeData data;

    public event Action OnShopChanged;

    [Header("Debug Preview")]
    public Renderer previewRenderer;
    public Color idleColor = Color.white;
    public Color healthColor = Color.green;
    public Color speedColor = Color.cyan;
    public Color damageColor = Color.red;
    public Color bulletTypeColor = Color.yellow;
    public Color missilesColor = Color.magenta;

    private Material runtimePreviewMaterial;

    private void Awake()
    {
        if (previewRenderer != null)
        {
            runtimePreviewMaterial = previewRenderer.material;
            runtimePreviewMaterial.color = idleColor;
        }

        NotifyShopChanged();
    }

    private bool CanAfford(int cost)
    {
        return data != null && data.points >= cost;
    }

    private bool TrySpend(int cost)
    {
        if (!CanAfford(cost))
        {
            return false;
        }

        data.points -= cost;
        return true;
    }

    private void NotifyShopChanged()
    {
        OnShopChanged?.Invoke();
    }

    private void SetDebugColor(Color color, int level)
    {
        if (runtimePreviewMaterial == null)
        {
            return;
        }

        float intensity = Mathf.Clamp01(0.35f + (level * 0.12f));
        runtimePreviewMaterial.color = Color.Lerp(idleColor, color, intensity);
    }

    private void SetDebugColor(Color color)
    {
        if (runtimePreviewMaterial == null)
        {
            return;
        }

        runtimePreviewMaterial.color = color;
    }

    public void BuySpeed()
    {
        if (data == null)
        {
            return;
        }

        if (data.speedLevel >= data.maxSpeedLevel)
        {
            return;
        }

        int cost = data.GetSpeedCost();
        if (TrySpend(cost))
        {
            data.speedLevel++;
            SetDebugColor(speedColor, data.speedLevel);
            NotifyShopChanged();
        }
    }

    public void BuyHealth()
    {
        if (data == null)
        {
            return;
        }

        if (data.healthLevel >= data.maxHealthLevel)
        {
            return;
        }

        int cost = data.GetHealthCost();
        if (TrySpend(cost))
        {
            data.healthLevel++;
            SetDebugColor(healthColor, data.healthLevel);
            NotifyShopChanged();
        }
    }

    public void BuyDamage()
    {
        if (data == null)
        {
            return;
        }

        if (data.damageLevel >= data.maxDamageLevel)
        {
            return;
        }

        int cost = data.GetDamageCost();
        if (TrySpend(cost))
        {
            data.damageLevel++;
            SetDebugColor(damageColor, data.damageLevel);
            NotifyShopChanged();
        }
    }

    public void BuyBulletType()
    {
        if (data == null)
        {
            return;
        }

        if (data.bulletType == "Advanced")
        {
            return;
        }

        if (TrySpend(data.bulletTypeUpgradeCost))
        {
            data.bulletType = "Advanced";
            SetDebugColor(bulletTypeColor);
            NotifyShopChanged();
        }
    }

    public void BuyMissiles()
    {
        if (data == null)
        {
            return;
        }

        if (data.missilesUnlocked)
        {
            return;
        }

        if (TrySpend(data.missileUnlockCost))
        {
            data.missilesUnlocked = true;
            SetDebugColor(missilesColor);
            NotifyShopChanged();
        }
    }
}