using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace WeaponCustomizer;

public static class ApplyPatches
{
    public static void Enable()
    {
        new InsertModPatch().Enable();
        new IconPatch().Enable();
    }

    public class InsertModPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass735.GClass736), nameof(GClass735.GClass736.InsertItem));
        }

        [PatchPostfix]
        public static void Postfix(GClass735.GClass736 __instance)
        {
            if (__instance.Item.GetRootItemNotEquipment() is not Weapon weapon || __instance.Item.Parent.Container is not Slot parentSlot)
            {
                return;
            }

            if (Customizations.IsCustomized(weapon.Id, parentSlot.FullId, out CustomPosition customPosition))
            {
                Logger.LogInfo($"WC: Updating {__instance.Bone.name} localPosition from ({__instance.Bone.localPosition.x}, {__instance.Bone.localPosition.y}, {__instance.Bone.localPosition.z}) to ({customPosition.Position.x}, {customPosition.Position.y}, {customPosition.Position.z})");
                __instance.Bone.localPosition = customPosition.Position;
            }
        }
    }

    // Force the weapon icon to be regenerated
    public class IconPatch : ModulePatch
    {
        public static readonly HashSet<string> WeaponIdsToRefresh = [];

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass894), nameof(GClass894.GetItemIcon));
        }

        [PatchPrefix]
        public static void Prefix(Item item, ref bool forcedGeneration)
        {
            if (WeaponIdsToRefresh.Contains(item.Id))
            {
                forcedGeneration = true;
                WeaponIdsToRefresh.Remove(item.Id);
            }
        }
    }
}