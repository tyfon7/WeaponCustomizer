using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace WeaponCustomizer;

public static class EditPatches
{
    private static readonly string[] MovableMods =
    [
        "mod_foregrip",
        "mod_stock",
        "mod_stock_000",
        "mod_stock_001",
        "mod_stock_002",
        "mod_stock_akms",
        "mod_sight_front",
        "mod_sight_rear",
        "mod_scope",
        "mod_scope_000",
        "mod_scope_001",
        "mod_scope_002",
        "mod_scope_003",
        "mod_tactical",
        "mod_tactical_000",
        "mod_tactical_001",
        "mod_tactical_002",
        "mod_tactical_003",
        "mod_mount",
        "mod_mount_000",
        "mod_mount_001",
        "mod_mount_002"
    ];

    public static void Enable()
    {
        new BoneMoverPatch().Enable();
    }

    public class BoneMoverPatch : ModulePatch
    {
        private static MethodInfo UpdatePositionsMethod;
        private static FieldInfo ViewporterField;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ModdingScreenSlotView), nameof(ModdingScreenSlotView.Show));
        }

        [PatchPostfix]
        public static void Postfix(
            Image ____boneIcon,
            Transform ___transform_0,
            GInterface444 ___ginterface444_0,
            CompoundItem ___compoundItem_0,
            Slot ___slot_0)
        {
            if (!MovableMods.Contains(___transform_0.name) || ___compoundItem_0 is not Weapon weapon)
            {
                return;
            }

            UpdatePositionsMethod = AccessTools.Method(___ginterface444_0.GetType(), "UpdatePositions");
            ViewporterField = AccessTools.Field(___ginterface444_0.GetType(), "_viewporter");
            var viewporter = ViewporterField.GetValue(___ginterface444_0) as CameraViewporter;

            ____boneIcon.GetOrAddComponent<DraggableBone>().Init(
                ____boneIcon,
                ___transform_0,
                weapon,
                ___slot_0.FullId,
                viewporter,
                () => UpdatePositionsMethod.Invoke(___ginterface444_0, []));
        }
    }
}