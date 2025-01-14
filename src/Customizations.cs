using EFT.InventoryLogic;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace WeaponCustomizer;

public static class Customizations
{
    private static readonly ConditionalWeakTable<Weapon, Dictionary<string, CustomPosition>> WeaponCustomizations = new();
    private static readonly ConditionalWeakTable<Preset, Dictionary<string, CustomPosition>> PresetCustomizations = new();

    public static bool IsCustomized(this Weapon weapon, out Dictionary<string, CustomPosition> slots)
    {
        if (WeaponCustomizations.TryGetValue(weapon, out slots))
        {
            return slots.Count > 0;
        }

        return false;
    }

    public static bool IsCustomized(this Preset preset, out Dictionary<string, CustomPosition> slots)
    {
        if (PresetCustomizations.TryGetValue(preset, out slots))
        {
            return slots.Count > 0;
        }

        return false;
    }

    public static bool IsCustomized(this Weapon weapon, string slotId, out CustomPosition customPosition)
    {
        if (WeaponCustomizations.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots) &&
            slots.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static bool IsCustomized(this Preset preset, string slotId, out CustomPosition customPosition)
    {
        if (PresetCustomizations.TryGetValue(preset, out Dictionary<string, CustomPosition> slots) &&
            slots.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static void SetCustomization(this Weapon weapon, string slotId, CustomPosition customPosition)
    {
        var slots = WeaponCustomizations.GetOrCreateValue(weapon);
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
        if (WeaponCustomizations.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            slots.Clear();
            WeaponCustomizations.Remove(weapon);

            // Only save the actual guns, not copies in the edit build screen
            if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
            {
                SaveCustomizations(weapon.Id, null);
            }
        }
    }

    public static void ResetCustomization(this Weapon weapon, string slotId)
    {
        if (WeaponCustomizations.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            slots.Remove(slotId);
            if (slots.Count == 0)
            {
                WeaponCustomizations.Remove(weapon);
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
        WeaponCustomizations.Remove(to);
        if (WeaponCustomizations.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            WeaponCustomizations.Add(to, slots);
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
        if (WeaponCustomizations.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            WeaponCustomizations.Remove(weapon);
            WeaponCustomizations.Add(weapon, new Dictionary<string, CustomPosition>(slots));
        }
    }

    public static void CopyCustomizations(this Weapon weapon, Weapon to)
    {
        WeaponCustomizations.Remove(to);
        if (WeaponCustomizations.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            WeaponCustomizations.Add(to, new Dictionary<string, CustomPosition>(slots));
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            SaveCustomizations(to.Id, slots);
        }
    }

    public static void ApplyCustomizations(this Preset preset, Weapon to)
    {
        WeaponCustomizations.Remove(to);
        if (PresetCustomizations.TryGetValue(preset, out Dictionary<string, CustomPosition> slots))
        {
            WeaponCustomizations.Add(to, new Dictionary<string, CustomPosition>(slots));
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            SaveCustomizations(to.Id, slots);
        }
    }

    public static void SaveAsPreset(this Weapon weapon, Preset preset)
    {
        PresetCustomizations.Remove(preset);
        if (WeaponCustomizations.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            PresetCustomizations.Add(preset, new Dictionary<string, CustomPosition>(slots));
        }

        SaveCustomizations(preset.Id, slots);
    }

    public static void RemoveCustomizations(this Preset preset)
    {
        if (PresetCustomizations.Remove(preset))
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

    public static async Task LoadCustomizations(Inventory inventory, WeaponBuildsStorageClass weaponBuilds)
    {
        string payload = await RequestHandler.GetJsonAsync("/weaponcustomizer/load");
        var customizations = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CustomPositionJson>>>(payload);

        foreach (Item item in inventory.GetPlayerItems(EPlayerItems.NonQuestItems))
        {
            if (item is not Weapon weapon)
            {
                continue;
            }

            if (customizations.TryGetValue(weapon.Id, out Dictionary<string, CustomPositionJson> slots))
            {
                var customPositions = WeaponCustomizations.GetOrCreateValue(weapon);
                customPositions.Clear();

                foreach (var (slotId, customPosition) in slots)
                {
                    customPositions[slotId] = customPosition;
                }
            }
        }

        foreach (Preset preset in weaponBuilds.Dictionary_0.Values)
        {
            if (customizations.TryGetValue(preset.Id, out Dictionary<string, CustomPositionJson> slots))
            {
                var customPositions = PresetCustomizations.GetOrCreateValue(preset);
                customPositions.Clear();

                foreach (var (slotId, customPosition) in slots)
                {
                    customPositions[slotId] = customPosition;
                }

                if (preset.Item is Weapon weapon)
                {
                    preset.ApplyCustomizations(weapon);
                }
            }
        }
    }

    private struct SavePayload
    {
        public string id;
        public Dictionary<string, CustomPositionJson> slots;
    }

    private struct CustomPositionJson
    {
        public Vector3Json position;
        public Vector3Json original;

        public static implicit operator CustomPositionJson(CustomPosition c) => new() { position = c.Position, original = c.OriginalPosition };
        public static implicit operator CustomPosition(CustomPositionJson c) => new() { OriginalPosition = c.original, Position = c.position };
    }

    private struct Vector3Json
    {
        public float x;
        public float y;
        public float z;

        public static implicit operator Vector3Json(Vector3 v) => new() { x = v.x, y = v.y, z = v.z };
        public static implicit operator Vector3(Vector3Json v) => new(v.x, v.y, v.z);
    }
}