using EFT.InventoryLogic;
using Newtonsoft.Json;
using SPT.Common.Http;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace WeaponCustomizer;

public static class Customizations
{
    // Never remove the dictionary once added to a weapon, it may be shared with other clones of this weapon
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

        SaveCustomizations(weapon.Id, slots);
    }

    public static void ResetCustomization(this Weapon weapon)
    {
        // Clear the dictionary, don't remove it - clones still will have copies
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            slots.Clear();
            SaveCustomizations(weapon.Id, slots);
        }
    }

    public static void ResetCustomization(this Weapon weapon, string slotId)
    {
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            slots.Remove(slotId);
            SaveCustomizations(weapon.Id, slots);
        }
    }

    public static void ShareCustomization(this Weapon weapon, Weapon to)
    {
        if (ModPositions.TryGetValue(weapon, out Dictionary<string, CustomPosition> slots))
        {
            ModPositions.Add(to, slots);

            // Hypothesis: Cloned weapons are always for display purposes, only the original needs to be saved
        }
    }

    private static void SaveCustomizations(string weaponId, Dictionary<string, CustomPosition> slots)
    {
        SavePayload payload = new()
        {
            weaponId = weaponId,
            slots = []
        };

        foreach (var (slotId, customPosition) in slots)
        {
            payload.slots[slotId] = customPosition;
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