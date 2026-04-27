using System;
using UnityEngine;

public enum GunType
{
    MachineGun,
    BurstGun,
    HeavyCannon,
    RailCannon,
    LightGun,

    Missiles
}

public enum PlaneState
{
    Locked,
    Unlocked,
    Equipped
}

[Serializable]
public class PlaneData
{
    [SerializeField] private string planeId = string.Empty;
    [SerializeField] private string planeName = "New Plane";
    [SerializeField] private GunType gunType = GunType.MachineGun;
    [SerializeField] private int speed = 100;
    [SerializeField] private int health = 100;
    [SerializeField] private int damage = 25;
    [SerializeField] private int price = 100;
    [SerializeField] private PlaneState state = PlaneState.Locked;
    [SerializeField] private GameObject previewObject;
    [SerializeField] private GameObject planePrefab;

    public string PlaneId => planeId;
    public string PlaneName => planeName;
    public GunType GunType => gunType;
    public int Speed => speed;
    public int Health => health;
    public int Damage => damage;
    public int Price => price;
    public PlaneState State => state;
    public GameObject PlanePrefab => planePrefab;
    public GameObject PreviewObject => previewObject;

    public void Sanitize(int index)
    {
        if (string.IsNullOrWhiteSpace(planeId))
        {
            planeId = Guid.NewGuid().ToString("N");
        }

        if (string.IsNullOrWhiteSpace(planeName))
        {
            planeName = "Plane " + (index + 1);
        }

        speed = Mathf.Max(0, speed);
        health = Mathf.Max(0, health);
        damage = Mathf.Max(0, damage);
        price = Mathf.Max(0, price);
    }

    public void SetPlaneId(string newPlaneId)
    {
        planeId = newPlaneId;
    }

    public void SetState(PlaneState newState)
    {
        state = newState;
    }
}
