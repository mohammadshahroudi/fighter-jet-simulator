using UnityEngine;

public class PlayerUpgradeData : MonoBehaviour
{
    [Header("Currency")]
    [SerializeField] private int points = 50;

    [Header("Upgrade Levels")]
    [SerializeField] private int speedLevel = 0;
    [SerializeField] private int healthLevel = 0;
    [SerializeField] private int damageLevel = 0;

    [Header("Unlocks")]
    [SerializeField] private bool missilesUnlocked = false;
    [SerializeField] private string bulletType = "Basic";

    [Header("Base Upgrade Costs")]
    [SerializeField] private int baseSpeedCost = 10;
    [SerializeField] private int baseHealthCost = 10;
    [SerializeField] private int baseDamageCost = 10;

    [Header("Cost Increase Per Level")]
    [SerializeField] private int speedCostPerLevel = 5;
    [SerializeField] private int healthCostPerLevel = 5;
    [SerializeField] private int damageCostPerLevel = 5;

    [Header("One-Time Unlock Costs")]
    [SerializeField] private int bulletTypeUpgradeCost = 10;
    [SerializeField] private int missileUnlockCost = 25;

    [Header("Max Levels")]
    [SerializeField] private int maxSpeedLevel = 5;
    [SerializeField] private int maxHealthLevel = 5;
    [SerializeField] private int maxDamageLevel = 5;

    public int Points => points;
    public int SpeedLevel => speedLevel;
    public int HealthLevel => healthLevel;
    public int DamageLevel => damageLevel;
    public bool MissilesUnlocked => missilesUnlocked;
    public string BulletType => bulletType;
    public int BulletTypeUpgradeCost => bulletTypeUpgradeCost;
    public int MissileUnlockCost => missileUnlockCost;
    public int MaxSpeedLevel => maxSpeedLevel;
    public int MaxHealthLevel => maxHealthLevel;
    public int MaxDamageLevel => maxDamageLevel;

    private void OnValidate()
    {
        points = Mathf.Max(0, points);

        maxSpeedLevel = Mathf.Max(0, maxSpeedLevel);
        maxHealthLevel = Mathf.Max(0, maxHealthLevel);
        maxDamageLevel = Mathf.Max(0, maxDamageLevel);

        speedLevel = Mathf.Clamp(speedLevel, 0, maxSpeedLevel);
        healthLevel = Mathf.Clamp(healthLevel, 0, maxHealthLevel);
        damageLevel = Mathf.Clamp(damageLevel, 0, maxDamageLevel);

        baseSpeedCost = Mathf.Max(0, baseSpeedCost);
        baseHealthCost = Mathf.Max(0, baseHealthCost);
        baseDamageCost = Mathf.Max(0, baseDamageCost);

        speedCostPerLevel = Mathf.Max(0, speedCostPerLevel);
        healthCostPerLevel = Mathf.Max(0, healthCostPerLevel);
        damageCostPerLevel = Mathf.Max(0, damageCostPerLevel);

        bulletTypeUpgradeCost = Mathf.Max(0, bulletTypeUpgradeCost);
        missileUnlockCost = Mathf.Max(0, missileUnlockCost);

        if (string.IsNullOrWhiteSpace(bulletType))
        {
            bulletType = "Basic";
        }
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || points < amount)
        {
            return false;
        }

        points -= amount;
        return true;
    }

    public bool TryUpgradeSpeed()
    {
        if (speedLevel >= maxSpeedLevel)
        {
            return false;
        }

        int cost = GetSpeedCost();
        if (!TrySpend(cost))
        {
            return false;
        }

        speedLevel++;
        return true;
    }

    public bool TryUpgradeHealth()
    {
        if (healthLevel >= maxHealthLevel)
        {
            return false;
        }

        int cost = GetHealthCost();
        if (!TrySpend(cost))
        {
            return false;
        }

        healthLevel++;
        return true;
    }

    public bool TryUpgradeDamage()
    {
        if (damageLevel >= maxDamageLevel)
        {
            return false;
        }

        int cost = GetDamageCost();
        if (!TrySpend(cost))
        {
            return false;
        }

        damageLevel++;
        return true;
    }

    public bool TryUnlockBulletType()
    {
        if (bulletType == "Advanced")
        {
            return false;
        }

        if (!TrySpend(bulletTypeUpgradeCost))
        {
            return false;
        }

        bulletType = "Advanced";
        return true;
    }

    public bool TryUnlockMissiles()
    {
        if (missilesUnlocked)
        {
            return false;
        }

        if (!TrySpend(missileUnlockCost))
        {
            return false;
        }

        missilesUnlocked = true;
        return true;
    }

    public int GetSpeedCost()
    {
        return baseSpeedCost + (speedLevel * speedCostPerLevel);
    }

    public int GetHealthCost()
    {
        return baseHealthCost + (healthLevel * healthCostPerLevel);
    }

    public int GetDamageCost()
    {
        return baseDamageCost + (damageLevel * damageCostPerLevel);
    }
}