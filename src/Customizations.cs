using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Utils;

namespace WeaponCustomizer;

public static class Customizations
{
    private static readonly Dictionary<string, Dictionary<string, CustomPosition>> AllCustomizations = [];

    public static bool IsCustomized(this Weapon weapon)
    {
        return weapon.IsCustomized(out _);
    }

    public static bool IsCustomized(this Weapon weapon, out Dictionary<string, CustomPosition> slots)
    {
        if (AllCustomizations.TryGetValue(weapon.Id, out slots))
        {
            return slots.Count > 0;
        }

        return false;
    }

    public static bool IsCustomized(this Preset preset, out Dictionary<string, CustomPosition> slots)
    {
        if (AllCustomizations.TryGetValue(preset.Id, out slots))
        {
            return slots.Count > 0;
        }

        return false;
    }

    public static bool IsCustomized(this Weapon weapon, string slotId, out CustomPosition customPosition)
    {
        if (AllCustomizations.TryGetValue(weapon.Id, out var slots) &&
            slots.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static bool IsCustomized(this Preset preset, string slotId, out CustomPosition customPosition)
    {
        if (AllCustomizations.TryGetValue(preset.Id, out var slots) &&
            slots.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static void SetCustomization(this Weapon weapon, string slotId, CustomPosition customPosition)
    {
        if (!AllCustomizations.TryGetValue(weapon.Id, out var slots))
        {
            slots = AllCustomizations[weapon.Id] = [];
        }

        slots[slotId] = customPosition;

        // Only save the actual guns, not copies in the edit build screen
        if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            SaveCustomizations(weapon.Id, slots);
        }
    }

    public static void ResetCustomizations(this Weapon weapon)
    {
        // Clear the dictionary first, don't just remove it - clones still will have copies
        if (AllCustomizations.TryGetValue(weapon.Id, out var slots))
        {
            slots.Clear();
            AllCustomizations.Remove(weapon.Id);

            // Only save the actual guns, not copies in the edit build screen
            if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
            {
                SaveCustomizations(weapon.Id, null);
            }
        }
    }

    public static void ResetCustomization(this Weapon weapon, string slotId)
    {
        if (AllCustomizations.TryGetValue(weapon.Id, out var slots))
        {
            slots.Remove(slotId);
            if (slots.Count == 0)
            {
                AllCustomizations.Remove(weapon.Id);
                slots = null;
            }

            // Only save the actual guns, not copies in the edit build screen
            if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
            {
                SaveCustomizations(weapon.Id, slots);
            }
        }
    }

    // This is called by the clone patch, for icon generation, and also the modding screens
    public static void ShareCustomizations(this Weapon weapon, Weapon to)
    {
        if (weapon.Id == to.Id)
        {
            // CloneWithSameId causes this, no need to do anything
            return;
        }

        AllCustomizations.Remove(to.Id);
        if (AllCustomizations.TryGetValue(weapon.Id, out var slots))
        {
            // Multiple weapons now pointing to the same dictionary
            AllCustomizations.Add(to.Id, slots);
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            SaveCustomizations(to.Id, slots);
        }
    }

    // Used by the edit screen, to create a separate setting that will not automatically reflect back onto the original
    public static void UnshareCustomizations(this Weapon weapon)
    {
        if (AllCustomizations.TryGetValue(weapon.Id, out var slots))
        {
            // Clone old values, disconnecting it from any others (who are unaffected)
            AllCustomizations[weapon.Id] = new(slots);
        }
    }

    public static void CopyCustomizations(this Weapon weapon, Weapon to)
    {
        if (weapon.Id == to.Id)
        {
            return;
        }

        AllCustomizations.Remove(to.Id);
        if (AllCustomizations.TryGetValue(weapon.Id, out var slots))
        {
            // Clone
            AllCustomizations.Add(to.Id, new(slots));
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            SaveCustomizations(to.Id, slots);
        }
    }

    public static void ApplyCustomizations(this Preset preset, Weapon to)
    {
        AllCustomizations.Remove(to.Id);
        if (AllCustomizations.TryGetValue(preset.Id, out var slots))
        {
            // Clone
            AllCustomizations.Add(to.Id, new(slots));
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            SaveCustomizations(to.Id, slots);
        }
    }

    public static void SaveAsPreset(this Weapon weapon, Preset preset)
    {
        AllCustomizations.Remove(preset.Id);
        if (AllCustomizations.TryGetValue(weapon.Id, out var slots))
        {
            // Clone
            AllCustomizations.Add(preset.Id, new(slots));
        }

        SaveCustomizations(preset.Id, slots);
    }

    public static void RemoveCustomizations(this Preset preset)
    {
        if (AllCustomizations.Remove(preset.Id))
        {
            SaveCustomizations(preset.Id, null);
        }
    }

    public static bool CustomizationsMatch(this Weapon weapon, Weapon other)
    {
        if (weapon == null || other == null)
        {
            return false;
        }

        bool isCustomized = weapon.IsCustomized(out Dictionary<string, CustomPosition> customizations);
        bool otherCustomized = other.IsCustomized(out Dictionary<string, CustomPosition> otherCustomizations);

        if (isCustomized != otherCustomized)
        {
            return false;
        }

        if (!isCustomized)
        {
            return true;
        }

        return customizations.Count == otherCustomizations.Count && !customizations.Except(otherCustomizations).Any();
    }

    public static bool CustomizationsMatch(this Weapon weapon, Preset preset)
    {
        if (weapon == null || preset == null)
        {
            return false;
        }

        bool isCustomized = weapon.IsCustomized(out Dictionary<string, CustomPosition> customizations);
        bool otherCustomized = preset.IsCustomized(out Dictionary<string, CustomPosition> otherCustomizations);

        if (isCustomized != otherCustomized)
        {
            return false;
        }

        if (!isCustomized)
        {
            return true;
        }

        return customizations.Count == otherCustomizations.Count && !customizations.Except(otherCustomizations).Any();
    }

    private static void SaveCustomizations(string id, Dictionary<string, CustomPosition> slots)
    {
        SavePayload payload = new()
        {
            id = id,
            slots = []
        };

        if (slots != null)
        {
            foreach (var (slotId, customPosition) in slots)
            {
                payload.slots[slotId] = customPosition;
            }
        }

        RequestHandler.PutJsonAsync("/weaponcustomizer/save", JsonConvert.SerializeObject(payload));
    }

    public static async Task LoadCustomizations()
    {
        string payload = await RequestHandler.GetJsonAsync("/weaponcustomizer/load");
        var customizations = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CustomPositionJson>>>(payload);

        foreach (var (id, slots) in customizations)
        {
            var customPositions = AllCustomizations[id] = [];
            foreach (var (slotId, customPosition) in slots)
            {
                customPositions[slotId] = customPosition;
            }
        }
    }

    private struct SavePayload
    {
        public string id;
        public Dictionary<string, CustomPositionJson> slots;
    }
}