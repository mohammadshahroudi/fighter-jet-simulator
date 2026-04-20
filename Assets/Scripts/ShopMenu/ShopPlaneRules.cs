using System;
using System.Collections.Generic;

public static class ShopPlaneRules
{
    public static int GetEquippedPlaneIndex(IReadOnlyList<PlaneData> planes)
    {
        if (planes == null)
        {
            return -1;
        }

        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane != null && plane.State == PlaneState.Equipped)
            {
                return i;
            }
        }

        return -1;
    }

    public static int FindPlaneIndexById(IReadOnlyList<PlaneData> planes, string planeId)
    {
        if (string.IsNullOrWhiteSpace(planeId) || planes == null)
        {
            return -1;
        }

        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane != null && plane.PlaneId == planeId)
            {
                return i;
            }
        }

        return -1;
    }

    public static void EnsureUniquePlaneIds(IList<PlaneData> planes)
    {
        if (planes == null)
        {
            return;
        }

        HashSet<string> usedIds = new HashSet<string>();
        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane == null)
            {
                continue;
            }

            string id = plane.PlaneId;
            if (string.IsNullOrWhiteSpace(id) || usedIds.Contains(id))
            {
                id = Guid.NewGuid().ToString("N");
                plane.SetPlaneId(id);
            }

            usedIds.Add(id);
        }
    }

    public static void ForceEquipPlane(IList<PlaneData> planes, int planeIndex)
    {
        if (planes == null || planeIndex < 0 || planeIndex >= planes.Count)
        {
            return;
        }

        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane == null)
            {
                continue;
            }

            if (i == planeIndex)
            {
                plane.SetState(PlaneState.Equipped);
            }
            else if (plane.State == PlaneState.Equipped)
            {
                plane.SetState(PlaneState.Unlocked);
            }
        }
    }

    public static void EnsureSingleEquippedPlane(IList<PlaneData> planes)
    {
        if (planes == null || planes.Count == 0)
        {
            return;
        }

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

        if (equippedCount == 0)
        {
            int fallbackIndex = FindFallbackEquipIndex(planes);
            if (fallbackIndex >= 0)
            {
                ForceEquipPlane(planes, fallbackIndex);
            }

            return;
        }

        if (equippedCount == 1)
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

    public static int FindFallbackEquipIndex(IList<PlaneData> planes)
    {
        if (planes == null)
        {
            return -1;
        }

        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane != null && plane.State == PlaneState.Unlocked)
            {
                return i;
            }
        }

        for (int i = 0; i < planes.Count; i++)
        {
            if (planes[i] != null)
            {
                return i;
            }
        }

        return -1;
    }
}
