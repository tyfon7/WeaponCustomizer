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
        "mod_tactical001",
        "mod_tactical002",
        "mod_tactical_000",
        "mod_tactical_001",
        "mod_tactical_002",
        "mod_tactical_003",
        "mod_mount",
        "mod_mount_000",
        "mod_mount_001",
        "mod_mount_002",
        "mod_bipod"
    ];

    private static DefaultUIButton RevertButton;

    public static void Enable()
    {
        new BoneMoverPatch().Enable();
        new RevertButtonPatch().Enable();
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
                (done) =>
                {
                    UpdatePositionsMethod.Invoke(___ginterface444_0, []);
                    if (done)
                    {
                        if (weapon.IsCustomized(out _))
                        {
                            RevertButton.ShowGameObject();
                        }
                        else
                        {
                            RevertButton.HideGameObject();
                        }
                    }
                });
        }
    }

    public class RevertButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(WeaponModdingScreen).BaseType, nameof(WeaponModdingScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(MonoBehaviour __instance, Item item, DefaultUIButton ____backButton)
        {
            var foundTransform = __instance.transform.Find("RevertButton");
            if (foundTransform != null)
            {
                RevertButton = foundTransform.GetComponent<DefaultUIButton>();
            }
            else
            {
                RevertButton = UnityEngine.Object.Instantiate(____backButton, ____backButton.transform.parent, false);
                RevertButton.name = "RevertButton";
                RevertButton.SetHeaderText("Revert");

                RectTransform revertTransform = RevertButton.RectTransform();
                revertTransform.pivot = Vector2.one;
                revertTransform.anchorMin = revertTransform.anchorMax = Vector2.one;
                revertTransform.anchoredPosition = new Vector2(revertTransform.anchoredPosition.x, -25f);
            }

            RevertButton.OnClick.RemoveAllListeners();
            if (item is Weapon weapon)
            {
                RevertButton.OnClick.AddListener(() =>
                {
                    foreach (var bone in __instance.GetComponentsInChildren<DraggableBone>())
                    {
                        bone.Reset();
                    }
                });

                if (weapon.IsCustomized(out _))
                {
                    RevertButton.ShowGameObject();
                }
                else
                {
                    RevertButton.HideGameObject();
                }
            }
            else
            {
                RevertButton.HideGameObject();
            }
        }
    }
}