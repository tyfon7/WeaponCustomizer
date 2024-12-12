using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;
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
            ModdingScreenSlotView __instance,
            Image ____boneIcon,
            Transform ___transform_0,
            GInterface444 ___ginterface444_0,
            CompoundItem ___compoundItem_0,
            Slot ___slot_0,
            LineRenderer ____lineRenderer)
        {
            // if (!MovableMods.Contains(___transform_0.name))
            // {
            //     return;
            // }

            UpdatePositionsMethod = AccessTools.Method(___ginterface444_0.GetType(), "UpdatePositions");
            ViewporterField = AccessTools.Field(___ginterface444_0.GetType(), "_viewporter");
            var viewporter = ViewporterField.GetValue(___ginterface444_0) as CameraViewporter;
            var originalLocalPosition = ___transform_0.localPosition;

            // Load the offsets
            if (Customizations.Offsets.TryGetValue(___compoundItem_0.Id, out Dictionary<string, Vector3> offsets))
            {
                if (offsets.TryGetValue(___slot_0.FullId, out Vector3 offset))
                {
                    ___transform_0.localPosition += offset;
                    UpdatePositionsMethod.Invoke(___ginterface444_0, []);
                }
            }

            ____boneIcon.GetOrAddComponent<DraggableBone>().Init(___transform_0, ___compoundItem_0.Id, ___slot_0.FullId, viewporter, originalLocalPosition, () => UpdatePositionsMethod.Invoke(___ginterface444_0, []));
        }
    }

    public class DraggableBone : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private Transform mod;
        private string weaponId;
        private string slotId;
        private CameraViewporter viewporter;
        private Action updatePositions;

        private Vector3 originalLocalPosition;
        private Vector3 minLocalPosition;
        private Vector3 maxLocalPosition;
        private float maxDistance;
        private Vector2 minScreen;
        private Vector2 maxScreen;
        private Vector2 dragOffset;
        private bool reversed;

        public void Init(Transform mod, string weaponId, string slotId, CameraViewporter viewporter, Vector3 originalLocalPosition, Action updatePositions)
        {
            this.mod = mod;
            this.weaponId = weaponId;
            this.slotId = slotId;
            this.viewporter = viewporter;
            this.updatePositions = updatePositions;
            this.originalLocalPosition = originalLocalPosition;

            // Currently everything is locked to left-right axis (forward/backward on the gun)
            var direction = mod.parent.InverseTransformDirection(Vector3.right);

            // Arbitrary magnitude for the moment
            minLocalPosition = originalLocalPosition + (-0.1f * direction);
            maxLocalPosition = originalLocalPosition + (0.1f * direction);
            maxDistance = Vector3.Distance(minLocalPosition, maxLocalPosition);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                mod.localPosition = originalLocalPosition;

                updatePositions();

                if (Customizations.Offsets.TryGetValue(weaponId, out Dictionary<string, Vector3> offsets))
                {
                    offsets.Remove(slotId);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || mod.childCount <= 0)
            {
                // Cancel drag
                eventData.pointerDrag = null;
                return;
            }

            dragOffset = eventData.position - (Vector2)viewporter.TargetCamera.WorldToScreenPoint(mod.position);

            Vector3 minPosition = mod.parent.TransformPoint(minLocalPosition);
            Vector3 maxPosition = mod.parent.TransformPoint(maxLocalPosition);

            minScreen = viewporter.TargetCamera.WorldToScreenPoint(minPosition);
            maxScreen = viewporter.TargetCamera.WorldToScreenPoint(maxPosition);

            // Depending on gun rotation, might need to swap min and max
            if (reversed = minScreen.x > maxScreen.x)
            {
                (minScreen, maxScreen) = (maxScreen, minScreen);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Clamp the mouse position between the screen points
            Vector2 screenPosition = Vector2.Max(minScreen, Vector2.Min(maxScreen, eventData.position - dragOffset));
            float percentDelta = Vector2.Distance(minScreen, screenPosition) / Vector2.Distance(minScreen, maxScreen);

            mod.localPosition = reversed ?
                Vector3.MoveTowards(maxLocalPosition, minLocalPosition, percentDelta * maxDistance) :
                Vector3.MoveTowards(minLocalPosition, maxLocalPosition, percentDelta * maxDistance);

            updatePositions();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Customizations.Offsets.TryGetValue(weaponId, out Dictionary<string, Vector3> offsets))
            {
                offsets = [];
                Customizations.Offsets.Add(weaponId, offsets);
            }

            offsets[slotId] = mod.localPosition - originalLocalPosition;
        }
    }
}