using UnityEngine;

public class PlayerUpgradeData : MonoBehaviour
{
    [Header("Currency")]
    public int points = 50;

    [Header("Upgrade Levels")]
    public int speedLevel = 0;
    public int healthLevel = 0;
    public int damageLevel = 0;

    [Header("Unlocks")]
    public bool missilesUnlocked = false;
    public string bulletType = "Basic";

    [Header("Base Upgrade Costs")]
    public int baseSpeedCost = 10;
    public int baseHealthCost = 10;
    public int baseDamageCost = 10;

    [Header("Cost Increase Per Level")]
    public int speedCostPerLevel = 5;
    public int healthCostPerLevel = 5;
    public int damageCostPerLevel = 5;

    [Header("One-Time Unlock Costs")]
    public int bulletTypeUpgradeCost = 10;
    public int missileUnlockCost = 25;

    [Header("Max Levels")]
    public int maxSpeedLevel = 5;
    public int maxHealthLevel = 5;
    public int maxDamageLevel = 5;

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