 
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{


    [Header("Currency")]

    [Header("Planes")]
   [SerializeField] private PlaneDatabase planeDatabase;
    private List<PlaneData> planes => planeDatabase != null ? planeDatabase.allPlanes : null;
    [SerializeField] private int selectedPlaneIndex = 0;


    public event Action OnShopChanged;

    public int Money => money;
    public int SelectedPlaneIndex => selectedPlaneIndex;
    public IReadOnlyList<PlaneData> Planes => planes;

    public static int GetSavedEquippedPlaneIndex()
    {
        return ShopPersistence.GetSavedEquippedPlaneIndex();
    }

    public static string GetSavedEquippedPlaneId()
    {
        return ShopPersistence.GetSavedEquippedPlaneId();
    }

    private int money;

    private void Awake()
    {
        InitializeRuntimeState();
        ReloadMoneyFromPrefs(); // Always load from PlayerPrefs
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

    public int GetEquippedPlaneIndex()
    {
        return ShopPlaneRules.GetEquippedPlaneIndex(planes);
    }

    public PlaneData GetEquippedPlane()
    {
        int equippedIndex = GetEquippedPlaneIndex();
        if (equippedIndex < 0 || equippedIndex >= planes.Count)
        {
            return null;
        }

        return planes[equippedIndex];
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
                changed = false;
                break;
        }

        if (changed)
        {
            SaveRuntimeState();
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
                return "Equipped";
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

        if (plane.State == PlaneState.Equipped)
        {
            return false;
        }

        return true;
    }

    private void InitializeRuntimeState()
    {
        SanitizeSerializedData();
        LoadRuntimeState();
        SanitizeSerializedData();
    }

    private void SanitizeSerializedData()
    {
        // startingMoney removed; money is now always loaded from PlayerPrefs

        if (planes == null)
        {
           
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

        ShopPlaneRules.EnsureUniquePlaneIds(planes);

        if (planes.Count == 0)
        {
            selectedPlaneIndex = 0;
            return;
        }

        selectedPlaneIndex = Mathf.Clamp(selectedPlaneIndex, 0, planes.Count - 1);

        ShopPlaneRules.EnsureSingleEquippedPlane(planes);
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

    [ContextMenu("Reset Shop Save Data")]
    public void ResetSaveData()
    {
        ShopPersistence.ClearSaveData(planes);

        InitializeRuntimeState();
        NotifyShopChanged();
    }

    private void LoadRuntimeState()
    {
        money = ShopPersistence.LoadMoney();

        if (planes == null || planes.Count == 0)
        {
            selectedPlaneIndex = 0;
            return;
        }

        bool hasAnySavedState = false;
        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane == null)
            {
                continue;
            }

            PlaneState loadedState;
            if (!ShopPersistence.TryLoadPlaneState(plane.PlaneId, i, out loadedState))
            {
                continue;
            }

            plane.SetState(loadedState);
            hasAnySavedState = true;
        }

        selectedPlaneIndex = ShopPersistence.LoadSelectedPlaneIndex(selectedPlaneIndex);
        selectedPlaneIndex = Mathf.Clamp(selectedPlaneIndex, 0, planes.Count - 1);

        int equippedIndexFromId;
        bool hasEquippedById = ShopPersistence.TryLoadEquippedIndexById(planes, out equippedIndexFromId);
        int savedEquippedIndex = ShopPersistence.LoadLegacyEquippedIndex();
        int indexToEquip = equippedIndexFromId >= 0 ? equippedIndexFromId : savedEquippedIndex;

        if (indexToEquip >= 0 && indexToEquip < planes.Count)
        {
            ShopPlaneRules.ForceEquipPlane(planes, indexToEquip);
            selectedPlaneIndex = indexToEquip;
            hasAnySavedState = hasAnySavedState || hasEquippedById || savedEquippedIndex >= 0;
        }

        ShopPlaneRules.EnsureSingleEquippedPlane(planes);
        int equippedIndex = GetEquippedPlaneIndex();
        if (equippedIndex >= 0)
        {
            selectedPlaneIndex = equippedIndex;
        }

        if (!hasAnySavedState)
        {
            SaveRuntimeState();
        }
    }

    private void SaveRuntimeState()
    {
        ShopPlaneRules.EnsureSingleEquippedPlane(planes);
        ShopPersistence.SaveRuntimeState(money, selectedPlaneIndex, planes);
    }

    private void NotifyShopChanged()
    {
        OnShopChanged?.Invoke();
    }
    public void AddMoney(int amount)
    {
        money += amount;
        Debug.Log($"[ShopManager] Money updated: {money}"); // Debug log to verify money is updated
        ShopPersistence.SaveRuntimeState(money, selectedPlaneIndex, planes);
        NotifyShopChanged();
    }
       // Reload money from PlayerPrefs and notify UI
    public void ReloadMoneyFromPrefs()
    {
        money = PlayerPrefs.GetInt("ShopManager_Money", 0);
        NotifyShopChanged();
    }
}
