using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace WeaponCustomizer;

public static class ApplyPatches
{
    public static void Enable()
    {
        new InsertModPatch().Enable();
        new CreateItemPatch().Enable();
        new IconPatch().Enable();
    }

    public class InsertModPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // GClass735.GClass736
            Type ccvType = AccessTools.Field(typeof(Player.FirearmController), nameof(Player.FirearmController.CCV)).FieldType;
            Type type = AccessTools.Field(ccvType, nameof(Player.FirearmController.CCV.ContainerBones)).FieldType.GenericTypeArguments[1];

            return AccessTools.Method(type, "InsertItem");
        }

        [PatchPostfix]
        public static void Postfix(Item item, Transform ___Bone)
        {
            if (item.GetRootItemNotEquipment() is not Weapon weapon || item.Parent.Container is not Slot parentSlot)
            {
                return;
            }

            if (weapon.IsCustomized(parentSlot.FullId, out CustomPosition customPosition))
            {
                ___Bone.gameObject.GetOrAddComponent<CustomizedMod>().Init(customPosition);
            }
        }
    }

    // In some cases (creating the icon), the model is created and snapshotted before CustomizedMod can fix the positions
    // Force it to apply early here
    public class CreateItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PoolManager), nameof(PoolManager.CreateItemAsync));
        }

        [PatchPostfix]
        public static async void Postfix(Item item, Task<GameObject> __result)
        {
            if (item is Weapon weapon && weapon.IsCustomized())
            {
                GameObject root = await __result;
                foreach (var customizedMod in root.GetComponentsInChildren<CustomizedMod>())
                {
                    customizedMod.LateUpdate();
                }
            }
        }
    }

    // Include customizations in the item hash, so the icon cache knows when to update
    public class IconPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetItemHash") != null); // GClass896
            return AccessTools.Method(type, "GetItemHash");
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