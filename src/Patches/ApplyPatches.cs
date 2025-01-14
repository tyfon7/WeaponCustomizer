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

            if (weapon.IsCustomized(parentSlot.FullId, out CustomPosition customPosition))
            {
                //Logger.LogInfo($"WC: Updating {__instance.Bone.name} localPosition from ({__instance.Bone.localPosition.x}, {__instance.Bone.localPosition.y}, {__instance.Bone.localPosition.z}) to ({customPosition.Position.x}, {customPosition.Position.y}, {customPosition.Position.z})");
                __instance.Bone.localPosition = customPosition.Position;
            }
        }
    }

    // Include customizations in the item hash, so the icon cache knows when to update
    public class IconPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass896), nameof(GClass896.GetItemHash));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref int __result)
        {
            if (item is Weapon weapon && weapon.IsCustomized(out Dictionary<string, CustomPosition> slots))
            {
                foreach (var (key, value) in slots)
                {
                    __result *= 31 + key.GetHashCode();
                    __result *= 31 + value.GetHashCode();
                }
            }
        }
    }
}