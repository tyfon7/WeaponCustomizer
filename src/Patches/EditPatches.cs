using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Screens;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;
using UnityEngine.Events;
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
        "mod_tactical_004",
        "mod_mount",
        "mod_mount_000",
        "mod_mount_001",
        "mod_mount_002",
        "mod_mount_003",
        "mod_mount_004",
        "mod_mount_005",
        "mod_mount_006",
        "mod_bipod"
    ];

    private const string MultitoolId = "544fb5454bdc2df8738b456a";

    private static DefaultUIButton RevertButton;
    private static FieldInfo PresetField;

    public static void Enable()
    {
        PresetField = AccessTools.GetDeclaredFields(typeof(EditBuildScreen)).Single(f => f.FieldType == typeof(WeaponBuildClass));

        new BoneMoverPatch().Enable();
        new RevertButtonPatch().Enable();
        new AssemblePatch().Enable();
        new CheckIfAlreadyBuiltPatch().Enable();

        new LoadBuildPatch().Enable();
        new FindBuildPatch().Enable();
        new SaveBuildPatch().Enable();
        new RemoveBuildPatch().Enable();

        new ModdingIsActiveInRaidPatch().Enable();
        new ModdingIsInteractiveInRaidPatch().Enable();
        new ModWeaponInRaidPatch().Enable();
        new SimpleModdingScreenPatch().Enable();
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
            Slot slot,
            ModdingScreen moddingScreen,
            CompoundItem item,
            Transform modBone,
            Image ____boneIcon)
        {
            if (item is not Weapon weapon)
            {
                return;
            }

            if (!Settings.MoveEverything.Value && !MovableMods.Contains(modBone.name))
            {
                // May need to clean up if the draggable bone was already created
                var draggableBone = ____boneIcon.GetComponent<DraggableBone>();
                if (draggableBone != null)
                {
                    draggableBone.Reset();
                    UnityEngine.Object.Destroy(draggableBone);
                }
                else
                {
                    // Or if the mod was customized (otherwise there is no way to reset it)
                    var customizedMod = modBone.GetComponent<CustomizedMod>();
                    if (customizedMod != null)
                    {
                        customizedMod.Reset();
                        UnityEngine.Object.Destroy(customizedMod);
                    }

                    weapon.ResetCustomization(slot);
                    OnModMoved(weapon, moddingScreen);
                }

                return;
            }

            UpdatePositionsMethod = AccessTools.Method(moddingScreen.GetType(), "UpdatePositions");
            ViewporterField = AccessTools.Field(moddingScreen.GetType(), "_viewporter");
            var viewporter = ViewporterField.GetValue(moddingScreen) as CameraViewporter;

            ____boneIcon.GetOrAddComponent<DraggableBone>().Init(
                ____boneIcon,
                modBone,
                weapon,
                slot,
                viewporter,
                (done) =>
                {
                    UpdatePositionsMethod.Invoke(moddingScreen, []);
                    if (done)
                    {
                        OnModMoved(weapon, moddingScreen);
                    }
                });
        }

        private static void OnModMoved(Weapon weapon, ModdingScreen moddingScreen)
        {
            EditBuildScreen editBuildScreen = moddingScreen as EditBuildScreen;
            editBuildScreen?.CheckForVitalParts(); // Triggers assemble button

            if (weapon.IsCustomized())
            {
                RevertButton.ShowGameObject();
            }
            else
            {
                RevertButton.HideGameObject();
            }

            if (editBuildScreen != null && !weapon.CustomizationsMatch(PresetField.GetValue(editBuildScreen) as WeaponBuildClass))
            {
                editBuildScreen.method_34(); // Mark build as dirty
            }
        }
    }

    public class RevertButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(WeaponModdingScreen).BaseType, nameof(WeaponModdingScreen.UpdateItem));
        }

        [PatchPostfix]
        public static void Postfix(MonoBehaviour __instance, Item newItem, DefaultUIButton ____backButton)
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
            if (newItem is Weapon weapon)
            {
                RevertButton.OnClick.AddListener(OnClick(__instance));

                if (weapon.IsCustomized())
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

        private static UnityAction OnClick(MonoBehaviour screen)
        {
            return () =>
            {
                foreach (var bone in screen.GetComponentsInChildren<DraggableBone>())
                {
                    bone.Reset();
                }
            };
        }
    }

    public class AssemblePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("Assemble") != null); // GClass3259
            return AccessTools.Method(type, "Assemble");
        }

        // itemBody is the real weapon, buildWeapon is the temporary preset being applied to the itemBody
        [PatchPostfix]
        public static async void Postfix(Weapon itemBody, Weapon buildWeapon, Task<bool> __result)
        {
            if (!await __result)
            {
                return;
            }

            buildWeapon.CopyCustomizations(itemBody);
        }
    }

    public class CheckIfAlreadyBuiltPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("CheckIfAlreadyBuilt") != null); // GClass3259
            return AccessTools.Method(type, "CheckIfAlreadyBuilt");
        }

        [PatchPostfix]
        public static void Postfix(IEnumerable<Item> installedMods, Weapon assemblingWeapon, ref bool __result)
        {
            if (!__result || !installedMods.Any())
            {
                return;
            }

            // Figure out the gun body
            Item mod = installedMods.First();
            if (mod.GetRootMergedItem() is not Weapon itemBody)
            {
                Plugin.Instance.Logger.LogError("CheckIfAlreadyBuiltPatch failed to get weapon from mod list!");
                return;
            }

            __result = itemBody.CustomizationsMatch(assemblingWeapon);
        }
    }

    public class LoadBuildPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(EditBuildScreen), nameof(EditBuildScreen.UpdateItem));
        }

        [PatchPrefix]
        public static void Prefix(EditBuildScreen __instance, Item newItem)
        {
            if (newItem is not Weapon weapon)
            {
                return;
            }

            var preset = PresetField.GetValue(__instance) as WeaponBuildClass;
            if (preset == null)
            {
                return;
            }

            preset.ApplyCustomizations(weapon);
        }
    }

    // FindBuild looks for matching item templates. Assert that the customizations match too.
    // This means you can't have multiple builds with the same mods but different customizations.
    public class FindBuildPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponBuildsStorageClass), nameof(WeaponBuildsStorageClass.FindBuild));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref WeaponBuildClass __result)
        {
            if (__result == null || item is not Weapon weapon)
            {
                return;
            }

            if (weapon.CustomizationsMatch(__result))
            {
                return;
            }

            __result = null;
            return;
        }
    }

    public class SaveBuildPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponBuildsStorageClass), nameof(WeaponBuildsStorageClass.SaveBuild));
        }

        [PatchPostfix]
        public static void Postfix(WeaponBuildClass build)
        {
            if (build.Item is not Weapon weapon)
            {
                return;
            }

            weapon.SaveAsPreset(build);
        }
    }

    public class RemoveBuildPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponBuildsStorageClass), nameof(WeaponBuildsStorageClass.RemoveBuild));
        }

        [PatchPrefix]
        public static void Prefix(WeaponBuildsStorageClass __instance, MongoID buildId, ref WeaponBuildClass __state)
        {
            __instance.Dictionary_0.TryGetValue(buildId, out __state);
        }

        [PatchPostfix]
        public static async void Postfix(Task<IResult> __result, WeaponBuildClass __state)
        {
            if (__state != null && (await __result).Succeed)
            {
                __state.RemoveCustomizations();
            }
        }
    }

    public class ModdingIsActiveInRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsActive));
        }

        [PatchPostfix]
        public static void Postfix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
        {
            if (__instance.Item_0_1 is not Weapon || Settings.ModifyRaidWeapons.Value == ModRaidWeapon.Never)
            {
                return;
            }

            if (button == EItemInfoButton.Modding)
            {
                __result = true;
            }
        }
    }

    public class ModdingIsInteractiveInRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsInteractive));
        }

        [PatchPostfix]
        public static void Postfix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref IResult __result)
        {
            if (button == EItemInfoButton.Modding && Plugin.InRaid() && __instance.TraderControllerClass is InventoryController inventoryController)
            {
                if (inventoryController.ID == __instance.Item_0_1.Owner.ID && inventoryController.IsItemEquipped(__instance.Item_0_1))
                {
                    __result = new FailedResult("You can't edit equipped weapon");
                    return;
                }

                if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool && !inventoryController.Inventory.Equipment.GetAllItems().Any(i => i.TemplateId == MultitoolId))
                {
                    __result = new FailedResult("Inventory Errors/Not moddable without multitool");
                    return;
                }

                __result = SuccessfulResult.New;
            }
        }
    }

    public class ModWeaponInRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.ModWeapon));
        }

        [PatchPrefix]
        public static bool Prefix(ItemUiContext __instance, Item item, InventoryController ___inventoryController_0)
        {
            new WeaponModdingScreen.GClass3922(item, ___inventoryController_0, __instance.CompoundItem_0).ShowScreen(EScreenState.Queued);
            return false;
        }
    }

    public class SimpleModdingScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ModdingScreenSlotView), nameof(ModdingScreenSlotView.Show));
        }

        [PatchPostfix]
        public static void Postfix(ModdingScreenSlotView __instance, LineRenderer ____lineRenderer, Transform ____tooltipHoverArea)
        {
            if (!Plugin.InRaid())
            {
                return;
            }

            ____lineRenderer.gameObject.SetActive(false);
            ____tooltipHoverArea.gameObject.SetActive(false);
        }
    }
}