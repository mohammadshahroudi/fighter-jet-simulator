using System.Collections.Generic;
using UnityEngine;

public static class ShopPersistence
{
    private const string SaveKeyPrefix = "ShopManager";
    private const string MoneyKey = SaveKeyPrefix + "_Money";
    private const string SelectedPlaneIndexKey = SaveKeyPrefix + "_SelectedPlaneIndex";
    private const string EquippedPlaneIndexKey = SaveKeyPrefix + "_EquippedPlaneIndex";
    private const string EquippedPlaneIdKey = SaveKeyPrefix + "_EquippedPlaneId";
    private const string PlaneStateKeyPrefix = SaveKeyPrefix + "_PlaneState_";

    public static int GetSavedEquippedPlaneIndex()
    {
        return PlayerPrefs.GetInt(EquippedPlaneIndexKey, -1);
    }

    public static string GetSavedEquippedPlaneId()
    {
        return PlayerPrefs.GetString(EquippedPlaneIdKey, string.Empty);
    }

    public static void ClearSaveData(IReadOnlyList<PlaneData> planes)
    {
        PlayerPrefs.DeleteKey(MoneyKey);
        PlayerPrefs.DeleteKey(SelectedPlaneIndexKey);
        PlayerPrefs.DeleteKey(EquippedPlaneIndexKey);
        PlayerPrefs.DeleteKey(EquippedPlaneIdKey);

        if (planes != null)
        {
            for (int i = 0; i < planes.Count; i++)
            {
                PlayerPrefs.DeleteKey(GetLegacyPlaneStateKey(i));

                PlaneData plane = planes[i];
                if (plane == null || string.IsNullOrWhiteSpace(plane.PlaneId))
                {
                    continue;
                }

                PlayerPrefs.DeleteKey(GetPlaneStateKey(plane.PlaneId));
            }
        }

        PlayerPrefs.Save();
    }

    public static int LoadMoney()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MoneyKey, 0));
    }

    public static int LoadSelectedPlaneIndex(int fallbackSelectedIndex)
    {
        return PlayerPrefs.GetInt(SelectedPlaneIndexKey, fallbackSelectedIndex);
    }

    public static bool TryLoadPlaneState(string planeId, int legacyIndex, out PlaneState state)
    {
        state = PlaneState.Locked;

        string idKey = GetPlaneStateKey(planeId);
        string legacyKey = GetLegacyPlaneStateKey(legacyIndex);

        bool hasIdKey = PlayerPrefs.HasKey(idKey);
        bool hasLegacyKey = PlayerPrefs.HasKey(legacyKey);
        if (!hasIdKey && !hasLegacyKey)
        {
            return false;
        }

        int value = hasIdKey ? PlayerPrefs.GetInt(idKey) : PlayerPrefs.GetInt(legacyKey);
        if (!System.Enum.IsDefined(typeof(PlaneState), value))
        {
            return false;
        }

        state = (PlaneState)value;
        return true;
    }

    public static bool TryLoadEquippedIndexById(IReadOnlyList<PlaneData> planes, out int equippedIndex)
    {
        equippedIndex = ShopPlaneRules.FindPlaneIndexById(planes, GetSavedEquippedPlaneId());
        return equippedIndex >= 0;
    }

    public static int LoadLegacyEquippedIndex()
    {
        return PlayerPrefs.GetInt(EquippedPlaneIndexKey, -1);
    }

    public static void SaveRuntimeState(int money, int selectedPlaneIndex, IReadOnlyList<PlaneData> planes)
    {
        int equippedIndex = ShopPlaneRules.GetEquippedPlaneIndex(planes);

        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.SetInt(SelectedPlaneIndexKey, selectedPlaneIndex);
        PlayerPrefs.SetInt(EquippedPlaneIndexKey, equippedIndex);

        string equippedId = string.Empty;
        if (equippedIndex >= 0 && equippedIndex < planes.Count && planes[equippedIndex] != null)
        {
            equippedId = planes[equippedIndex].PlaneId;
        }

        PlayerPrefs.SetString(EquippedPlaneIdKey, equippedId);

        if (planes != null)
        {
            for (int i = 0; i < planes.Count; i++)
            {
                PlaneData plane = planes[i];
                if (plane == null)
                {
                    continue;
                }

                PlayerPrefs.SetInt(GetPlaneStateKey(plane.PlaneId), (int)plane.State);
                PlayerPrefs.SetInt(GetLegacyPlaneStateKey(i), (int)plane.State);
            }
        }

        PlayerPrefs.Save();
    }

    private static string GetPlaneStateKey(string planeId)
    {
        return PlaneStateKeyPrefix + planeId;
    }

    private static string GetLegacyPlaneStateKey(int index)
    {
        return PlaneStateKeyPrefix + index;
    }
}
