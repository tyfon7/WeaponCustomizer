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
            Type ccvType = AccessTools.Field(typeof(Player.FirearmController), nameof(Player.FirearmController.CCV)).FieldType; // GClass746
            Type type = AccessTools.Field(ccvType, nameof(Player.FirearmController.CCV.ContainerBones)).FieldType.GenericTypeArguments[1]; // GClass746.GClass747

            return AccessTools.Method(type, "InsertItem");
        }

        [PatchPostfix]
        public static void Postfix(Item item, Transform ___Bone)
        {
            if (item == null || ___Bone == null)
            {
                return;
            }

            if (item.GetRootItemNotEquipment() is not Weapon weapon || item.Parent.Container is not Slot parentSlot)
            {
                return;
            }

            if (weapon.IsCustomized(parentSlot, out Customization customization))
            {
                ___Bone.gameObject.GetOrAddComponent<CustomizedMod>().Init(customization);
            }
            else
            {
                // This bone is NOT customized, but was re-used from a pool. Remove the customization
                var customizedMod = ___Bone.gameObject.GetComponent<CustomizedMod>();
                if (customizedMod != null)
                {
                    UnityEngine.Object.DestroyImmediate(customizedMod);
                }
            }
        }
    }

    // In some cases (creating the icon), the model is created and snapshotted before CustomizedMod can fix the positions
    // Force it to apply early here
    public class CreateItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PoolManagerClass), nameof(PoolManagerClass.CreateItemAsync));
        }

        [PatchPostfix]
        public static async void Postfix(Item item, Task<GameObject> __result)
        {
            if (item is Weapon weapon && weapon.IsCustomized())
            {
                GameObject root = await __result;
                if (root == null)
                {
                    return;
                }

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
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetItemHash") != null); // GClass906
            return AccessTools.Method(type, "GetItemHash");
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref int __result)
        {
            if (item is Weapon weapon && weapon.IsCustomized(out Dictionary<string, Customization> slots))
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