using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Currency")]
    [SerializeField] private int startingMoney = 300;

    [Header("Planes")]
    [SerializeField] private List<PlaneData> planes = new List<PlaneData>();
    [SerializeField] private int selectedPlaneIndex = 0;

    public event Action OnShopChanged;

    public int Money => money;
    public int SelectedPlaneIndex => selectedPlaneIndex;
    public IReadOnlyList<PlaneData> Planes => planes;

    private int money;

    private void Awake()
    {
        InitializeRuntimeState();
        NotifyShopChanged();
    }

    private void OnValidate()
    {
        SanitizeSerializedData();
    }

    public PlaneData GetSelectedPlane()
    {
        if (planes == null || planes.Count == 0)
        {
            return null;
        }

        if (selectedPlaneIndex < 0 || selectedPlaneIndex >= planes.Count)
        {
            return null;
        }

        return planes[selectedPlaneIndex];
    }

    public void SelectPlane(int index)
    {
        if (planes == null || index < 0 || index >= planes.Count)
        {
            return;
        }

        selectedPlaneIndex = index;
        NotifyShopChanged();
    }

    public bool HandleSelectedPlaneAction()
    {
        PlaneData selected = GetSelectedPlane();
        if (selected == null)
        {
            return false;
        }

        bool changed = false;
        switch (selected.State)
        {
            case PlaneState.Locked:
                changed = TryUnlock(selected);
                break;
            case PlaneState.Unlocked:
                EquipPlane(selectedPlaneIndex);
                changed = true;
                break;
            case PlaneState.Equipped:
                selected.SetState(PlaneState.Unlocked);
                changed = true;
                break;
        }

        if (changed)
        {
            NotifyShopChanged();
        }

        return changed;
    }

    public string GetActionText(PlaneData plane)
    {
        if (plane == null)
        {
            return "N/A";
        }

        switch (plane.State)
        {
            case PlaneState.Locked:
                return "Unlock Plane";
            case PlaneState.Unlocked:
                return "Equip Plane";
            case PlaneState.Equipped:
                return "Unequip Plane";
            default:
                return "N/A";
        }
    }

    public bool IsActionAvailable(PlaneData plane)
    {
        if (plane == null)
        {
            return false;
        }

        if (plane.State == PlaneState.Locked)
        {
            return money >= plane.Price;
        }

        return true;
    }

    private void InitializeRuntimeState()
    {
        money = Mathf.Max(0, startingMoney);
        SanitizeSerializedData();
    }

    private void SanitizeSerializedData()
    {
        startingMoney = Mathf.Max(0, startingMoney);

        if (planes == null)
        {
            planes = new List<PlaneData>();
            selectedPlaneIndex = 0;
            return;
        }

        for (int i = 0; i < planes.Count; i++)
        {
            if (planes[i] == null)
            {
                continue;
            }

            planes[i].Sanitize(i);
        }

        if (planes.Count == 0)
        {
            selectedPlaneIndex = 0;
            return;
        }

        selectedPlaneIndex = Mathf.Clamp(selectedPlaneIndex, 0, planes.Count - 1);

        int equippedCount = 0;
        int equippedIndex = -1;

        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane == null || plane.State != PlaneState.Equipped)
            {
                continue;
            }

            equippedCount++;
            if (equippedIndex == -1)
            {
                equippedIndex = i;
            }
        }

        if (equippedCount <= 1)
        {
            return;
        }

        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane == null || i == equippedIndex)
            {
                continue;
            }

            if (plane.State == PlaneState.Equipped)
            {
                plane.SetState(PlaneState.Unlocked);
            }
        }
    }

    private bool TryUnlock(PlaneData plane)
    {
        if (plane == null || plane.State != PlaneState.Locked)
        {
            return false;
        }

        if (money < plane.Price)
        {
            return false;
        }

        money -= plane.Price;
        plane.SetState(PlaneState.Unlocked);
        return true;
    }

    private void EquipPlane(int planeIndex)
    {
        if (planes == null || planeIndex < 0 || planeIndex >= planes.Count)
        {
            return;
        }

        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane == null || plane.State != PlaneState.Equipped)
            {
                continue;
            }

            plane.SetState(PlaneState.Unlocked);
        }

        PlaneData selected = planes[planeIndex];
        if (selected != null)
        {
            selected.SetState(PlaneState.Equipped);
        }
    }

    private void NotifyShopChanged()
    {
        OnShopChanged?.Invoke();
    }
}
