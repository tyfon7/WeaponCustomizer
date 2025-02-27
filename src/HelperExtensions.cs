using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using SPT.Reflection.Utils;

namespace WeaponCustomizer;

public static class HelperExtensions
{
    public static bool IsCustomized(this Weapon weapon)
    {
        return weapon.IsCustomized(out _);
    }

    public static bool IsCustomized(this Weapon weapon, out Dictionary<string, Customization> slots)
    {
        if (Customizations.Database.TryGetValue(weapon.Id, out slots))
        {
            return slots.Count > 0;
        }

        return false;
    }

    public static bool IsCustomized(this Preset preset, out Dictionary<string, Customization> slots)
    {
        if (Customizations.Database.TryGetValue(preset.Id, out slots))
        {
            return slots.Count > 0;
        }

        return false;
    }

    public static bool IsCustomized(this Weapon weapon, string slotId, out Customization customPosition)
    {
        if (Customizations.Database.TryGetValue(weapon.Id, out var slots) &&
            slots.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static bool IsCustomized(this Preset preset, string slotId, out Customization customPosition)
    {
        if (Customizations.Database.TryGetValue(preset.Id, out var slots) &&
            slots.TryGetValue(slotId, out customPosition))
        {
            return true;
        }

        customPosition = default;
        return false;
    }

    public static void SetCustomization(this Weapon weapon, string slotId, Customization customPosition)
    {
        if (!Customizations.Database.TryGetValue(weapon.Id, out var slots))
        {
            slots = Customizations.Database[weapon.Id] = [];
        }

        slots[slotId] = customPosition;

        // Only save the actual guns, not copies in the edit build screen
        if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            Customizations.Save(weapon.Id, slots);
        }
    }

    public static void ResetCustomizations(this Weapon weapon)
    {
        // Clear the dictionary first, don't just remove it - clones still will have copies
        if (Customizations.Database.TryGetValue(weapon.Id, out var slots))
        {
            slots.Clear();
            Customizations.Database.Remove(weapon.Id);

            // Only save the actual guns, not copies in the edit build screen
            if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
            {
                Customizations.Save(weapon.Id, null);
            }
        }
    }

    public static void ResetCustomization(this Weapon weapon, string slotId)
    {
        if (Customizations.Database.TryGetValue(weapon.Id, out var slots))
        {
            slots.Remove(slotId);
            if (slots.Count == 0)
            {
                Customizations.Database.Remove(weapon.Id);
                slots = null;
            }

            // Only save the actual guns, not copies in the edit build screen
            if (weapon.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
            {
                Customizations.Save(weapon.Id, slots);
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

        Customizations.Database.Remove(to.Id);
        if (Customizations.Database.TryGetValue(weapon.Id, out var slots))
        {
            // Multiple weapons now pointing to the same dictionary
            Customizations.Database.Add(to.Id, slots);
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            Customizations.Save(to.Id, slots);
        }
    }

    // Used by the edit screen, to create a separate setting that will not automatically reflect back onto the original
    public static void UnshareCustomizations(this Weapon weapon)
    {
        if (Customizations.Database.TryGetValue(weapon.Id, out var slots))
        {
            // Clone old values, disconnecting it from any others (who are unaffected)
            Customizations.Database[weapon.Id] = new(slots);
        }
    }

    public static void CopyCustomizations(this Weapon weapon, Weapon to)
    {
        if (weapon.Id == to.Id)
        {
            return;
        }

        Customizations.Database.Remove(to.Id);
        if (Customizations.Database.TryGetValue(weapon.Id, out var slots))
        {
            // Clone
            Customizations.Database.Add(to.Id, new(slots));
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            Customizations.Save(to.Id, slots);
        }
    }

    public static void ApplyCustomizations(this Preset preset, Weapon to)
    {
        Customizations.Database.Remove(to.Id);
        if (Customizations.Database.TryGetValue(preset.Id, out var slots))
        {
            // Clone
            Customizations.Database.Add(to.Id, new(slots));
        }

        // Only save the actual guns, not copies in the edit build screen
        if (to.Owner?.ID == PatchConstants.BackEndSession.Profile.Id)
        {
            Customizations.Save(to.Id, slots);
        }
    }

    public static void SaveAsPreset(this Weapon weapon, Preset preset)
    {
        Customizations.Database.Remove(preset.Id);
        if (Customizations.Database.TryGetValue(weapon.Id, out var slots))
        {
            // Clone
            Customizations.Database.Add(preset.Id, new(slots));
        }

        Customizations.Save(preset.Id, slots);
    }

    public static void RemoveCustomizations(this Preset preset)
    {
        if (Customizations.Database.Remove(preset.Id))
        {
            Customizations.Save(preset.Id, null);
        }
    }

    public static bool CustomizationsMatch(this Weapon weapon, Weapon other)
    {
        if (weapon == null || other == null)
        {
            return false;
        }

        bool isCustomized = weapon.IsCustomized(out Dictionary<string, Customization> customizations);
        bool otherCustomized = other.IsCustomized(out Dictionary<string, Customization> otherCustomizations);

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

        bool isCustomized = weapon.IsCustomized(out Dictionary<string, Customization> customizations);
        bool otherCustomized = preset.IsCustomized(out Dictionary<string, Customization> otherCustomizations);

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
}