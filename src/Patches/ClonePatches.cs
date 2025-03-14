using System;
using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace WeaponCustomizer;

public static class ClonePatches
{
    public static void Enable()
    {
        new ClonePatch().Enable();
        new SplitPresetPatch().Enable();
    }

    public class ClonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("IncompatibleByMalfunction") != null); // GClass3176
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m =>
            {
                var parameters = m.GetParameters();
                return parameters[0].Name == "originalItem" && parameters.Length > 2;
            }).MakeGenericMethod([typeof(Item)]);
        }

        [PatchPostfix]
        public static void Postfix(Item originalItem, ref Item __result)
        {
            if (originalItem is not Weapon weapon || __result is not Weapon to)
            {
                return;
            }

            weapon.ShareCustomizations(to);
        }
    }

    // Split the customizations off the edit build screen's gun away from the underlying player's gun. They will only be applied if the user clicks assemble
    public class SplitPresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(
                typeof(EditBuildScreen),
                nameof(EditBuildScreen.Show),
                [typeof(Item), typeof(Item), typeof(InventoryController), typeof(ISession)]);
        }

        [PatchPostfix]
        public static void Postfix(Item buildItem)
        {
            if (buildItem is Weapon weapon)
            {
                weapon.UnshareCustomizations();
            }
        }
    }
}