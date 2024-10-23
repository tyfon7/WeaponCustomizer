using System;
using System.Linq;
using System.Reflection;
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
        "mod_sight_rear",
        "mod_scope",
        "mod_tactical",
        "mod_tactical_000",
        "mod_tactical_001",
        "mod_tactical_002",
        "mod_sight_front",
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
        public static void Postfix(ModdingScreenSlotView __instance, Image ____boneIcon, Transform ___transform_0, GInterface386 ___ginterface386_0)
        {
            if (!MovableMods.Contains(___transform_0.name))
            {
                return;
            }

            UpdatePositionsMethod = AccessTools.Method(___ginterface386_0.GetType(), "UpdatePositions");
            ViewporterField = AccessTools.Field(___ginterface386_0.GetType(), "_viewporter");
            var viewporter = ViewporterField.GetValue(___ginterface386_0) as CameraViewporter;

            ____boneIcon.GetOrAddComponent<DraggableBone>().Init(___transform_0, viewporter, () => UpdatePositionsMethod.Invoke(___ginterface386_0, []));
        }
    }

    enum Axis
    {
        Y,
        Z
    }

    public class DraggableBone : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private Transform mod;
        private CameraViewporter viewporter;
        private Action updatePositions;
        private bool reversed = false;

        private Axis axis;
        private float originalValue;
        private float localMinValue;
        private float localMaxValue;
        private float screenMinX;
        private float screenMaxX;

        public void Init(Transform mod, CameraViewporter viewporter, Action updatePositions)
        {
            this.mod = mod;
            this.viewporter = viewporter;
            this.updatePositions = updatePositions;

            float rotation = Mathf.RoundToInt(mod.localRotation.eulerAngles.x);
            switch (rotation)
            {
                case 0:
                case 180:
                    axis = Axis.Y;
                    originalValue = mod.localPosition.y;
                    break;
                case 90:
                case 270:
                default:
                    axis = Axis.Z;
                    originalValue = mod.localPosition.z;
                    break;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                switch (axis)
                {
                    case Axis.Y:
                        mod.localPosition = new(mod.localPosition.x, originalValue, mod.localPosition.z);
                        break;
                    case Axis.Z:
                        mod.localPosition = new(mod.localPosition.x, mod.localPosition.y, originalValue);
                        break;
                }

                updatePositions();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || mod.childCount <= 0)
            {
                eventData.pointerDrag = null;
                return;
            }

            localMinValue = originalValue - 0.25f;
            localMaxValue = originalValue + 0.25f;
            Vector3 localMin = axis switch
            {
                Axis.Y => new(mod.localPosition.x, localMinValue, mod.localPosition.z),
                Axis.Z => new(mod.localPosition.x, mod.localPosition.y, localMinValue)
            };

            Vector3 localMax = axis switch
            {
                Axis.Y => new(mod.localPosition.x, localMaxValue, mod.localPosition.z),
                Axis.Z => new(mod.localPosition.x, mod.localPosition.y, localMaxValue)
            };

            Vector3 worldMin = mod.parent.TransformPoint(localMin);
            Vector3 worldMax = mod.parent.TransformPoint(localMax);

            Vector3 screenMin = viewporter.TargetCamera.WorldToScreenPoint(worldMin);
            Vector3 screenMax = viewporter.TargetCamera.WorldToScreenPoint(worldMax);

            // Depending on gun rotation, might need to swap min and max
            reversed = screenMin.x > screenMax.x;

            screenMinX = reversed ? screenMax.x : screenMin.x;
            screenMaxX = reversed ? screenMin.x : screenMax.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float positionX = Mathf.Clamp(eventData.position.x, screenMinX, screenMaxX);
            float screenDelta = (positionX - screenMinX) / (screenMaxX - screenMinX);

            float newLocalValue = reversed ?
                localMaxValue - screenDelta * (localMaxValue - localMinValue) :
                localMinValue + screenDelta * (localMaxValue - localMinValue);

            mod.localPosition = axis switch
            {
                Axis.Y => new(mod.localPosition.x, newLocalValue, mod.localPosition.z),
                Axis.Z => new(mod.localPosition.x, mod.localPosition.y, newLocalValue)
            };

            updatePositions();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }
    }
}