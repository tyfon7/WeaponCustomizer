using System.Collections.Generic;

namespace WeaponCustomizer;

public static class Customizations
{
    private static Dictionary<string, Dictionary<string, CustomPosition>> ModPositions = [];

    public static bool IsCustomized(string weaponId, string slotId, out CustomPosition customPosition)
    {
        if (ModPositions.TryGetValue(weaponId, out Dictionary<string, CustomPosition> customPositions) &&
            customPositions.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static void Set(string weaponId, string slotId, CustomPosition customPosition)
    {
        if (!ModPositions.TryGetValue(weaponId, out Dictionary<string, CustomPosition> customPositions))
        {
            customPositions = [];
            ModPositions[weaponId] = customPositions;
        }

        customPositions[slotId] = customPosition;

        RefreshIcon(weaponId);
    }

    public static void Reset(string weaponId, string slotId)
    {
        if (ModPositions.TryGetValue(weaponId, out Dictionary<string, CustomPosition> customPositions))
        {
            customPositions.Remove(slotId);
            if (customPositions.Count == 0)
            {
                ModPositions.Remove(weaponId);
            }

            RefreshIcon(weaponId);
        }
    }

    public static void Copy(string fromWeaponId, string toWeaponId)
    {
        if (ModPositions.TryGetValue(fromWeaponId, out Dictionary<string, CustomPosition> customPositions))
        {
            ModPositions[toWeaponId] = customPositions;
            RefreshIcon(toWeaponId);
        }
    }

    public static void RefreshIcon(string weaponId)
    {
        ApplyPatches.IconPatch.WeaponIdsToRefresh.Add(weaponId);
    }

}