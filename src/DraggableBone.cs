using System;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeaponCustomizer;

public class DraggableBone : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float MOVE_DISTANCE = 0.5f;
    private Image boneIcon;
    private Transform mod;
    private CustomizedMod customizedMod;
    private Weapon weapon;
    private string slotId;
    private CameraViewporter viewporter;
    private Action<bool> onChange;

    private Vector3 originalLocalPosition;
    private Vector3 minLocalPosition;
    private Vector3 maxLocalPosition;
    private Vector2 minScreen;
    private Vector2 maxScreen;
    private Transform rotator;
    private Plane weaponPlane;

    private bool dragging;
    private bool hovered;

    public void Init(Image boneIcon, Transform mod, Weapon weapon, string slotId, CameraViewporter viewporter, Action<bool> onChange)
    {
        this.boneIcon = boneIcon;
        this.mod = mod;
        this.weapon = weapon;
        this.slotId = slotId;
        this.viewporter = viewporter;
        this.onChange = onChange;

        if (weapon.IsCustomized(slotId, out CustomPosition customPosition))
        {
            originalLocalPosition = customPosition.OriginalPosition;
        }
        else
        {
            originalLocalPosition = mod.localPosition;
        }

        // These bones get reinitialized when attachments are added, so we can't assume the gun is straight. The rotator has the rotation
        rotator = mod.root.Find("Rotator");

        // Currently everything is locked to left-right axis (forward/backward on the gun)
        var direction = mod.parent.InverseTransformDirection(rotator.right);

        // Arbitrary magnitude for the moment
        minLocalPosition = originalLocalPosition - (MOVE_DISTANCE * direction);
        maxLocalPosition = originalLocalPosition + (MOVE_DISTANCE * direction);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        SetColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        SetColor();
    }

    private void SetColor()
    {
        if (mod.childCount > 0 && (hovered || dragging))
        {
            boneIcon.color = Color.cyan;
        }
        else
        {
            boneIcon.color = Color.white;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Reset();
        }
    }

    public void Reset()
    {
        var customizedMod = mod.GetComponent<CustomizedMod>();
        if (customizedMod != null)
        {
            customizedMod.Reset();
            Destroy(customizedMod);
            customizedMod = null;
        }

        weapon.ResetCustomization(slotId);

        onChange(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || mod.childCount <= 0)
        {
            // Cancel drag
            eventData.pointerDrag = null;
            return;
        }

        dragging = true;
        SetColor();

        customizedMod = mod.GetComponent<CustomizedMod>();
        if (customizedMod == null)
        {
            customizedMod = mod.gameObject.AddComponent<CustomizedMod>();
            customizedMod.Init(originalLocalPosition, mod.localPosition);
        }

        Vector3 minPosition = mod.parent.TransformPoint(minLocalPosition);
        Vector3 maxPosition = mod.parent.TransformPoint(maxLocalPosition);

        minScreen = viewporter.TargetCamera.WorldToScreenPoint(minPosition);
        maxScreen = viewporter.TargetCamera.WorldToScreenPoint(maxPosition);

        weaponPlane = new Plane(rotator.forward, mod.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 mouseVector = eventData.position - minScreen;
        Vector2 allowedVector = maxScreen - minScreen;

        // This gets the amount of a vector A (the mouse position vector) that applies to vector B (the allowed positions of the mod)
        // Which is to say, helps find the point where A projects onto B, aka the closest point on B from the tip of A
        float projectedMagnitude = Vector2.Dot(mouseVector, allowedVector) / allowedVector.magnitude;
        projectedMagnitude = Mathf.Clamp(projectedMagnitude, 0, allowedVector.magnitude);

        if (Settings.StepSize.Value > 0)
        {
            projectedMagnitude = Mathf.RoundToInt(projectedMagnitude / Settings.StepSize.Value) * Settings.StepSize.Value;
        }

        Vector2 screenPosition = Vector2.MoveTowards(minScreen, maxScreen, projectedMagnitude);

        // With that perfect screen position, raycast onto the weapon plane to find the exact spot where the mod should go
        Ray ray = viewporter.TargetCamera.ScreenPointToRay(screenPosition);
        if (weaponPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 localHitPoint = mod.parent.InverseTransformPoint(hitPoint);

            customizedMod.Move(localHitPoint);
        }

        onChange(false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        SetColor();

        weapon.SetCustomization(slotId, customizedMod.Customization);

        onChange(true);
    }
}
