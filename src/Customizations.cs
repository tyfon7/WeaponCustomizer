using EFT.InventoryLogic;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace WeaponCustomizer;

public static class Customizations
{
    private static readonly ConditionalWeakTable<Weapon, Dictionary<string, CustomPosition>> ModPositions = new();

    public static bool IsCustomized(this Weapon weapon, out Dictionary<string, CustomPosition> slots)
    {
        if (ModPositions.TryGetValue(weapon, out slots))
        {
            return slots.Count > 0;
        }

        return false;
    }

    public static bool IsCustomized(this Weapon weapon, string slotId, out CustomPosition customPosition)
    {
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots) &&
            slots.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static void SetCustomization(this Weapon weapon, string slotId, CustomPosition customPosition)
    {
        var slots = ModPositions.GetOrCreateValue(weapon);
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
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            slots.Clear();
            ModPositions.Remove(weapon);

            // Only save the actual guns, not copies in the edit build screen
            if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
            {
                SaveCustomizations(weapon.Id, null);
            }
        }
    }

    public static void ResetCustomization(this Weapon weapon, string slotId)
    {
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            slots.Remove(slotId);
            if (slots.Count == 0)
            {
                ModPositions.Remove(weapon);
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
        ModPositions.Remove(to);
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            ModPositions.Add(to, slots);
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
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            ModPositions.Remove(weapon);
            ModPositions.Add(weapon, new Dictionary<string, CustomPosition>(slots));
        }
    }

    private static void SaveCustomizations(string weaponId, Dictionary<string, CustomPosition> slots)
    {
        SavePayload payload = new()
        {
            weaponId = weaponId,
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

    public static async Task LoadCustomizations(Inventory inventory)
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
                var customPositions = ModPositions.GetOrCreateValue(weapon);
                customPositions.Clear();

                foreach (var (slotId, customPosition) in slots)
                {
                    customPositions[slotId] = customPosition;
                }
            }
        }
    }

    private struct SavePayload
    {
        public string weaponId;
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