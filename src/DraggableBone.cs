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
    private float maxDistance;
    private Vector2 minScreen;
    private Vector2 maxScreen;
    private Vector2 dragOffset;
    private bool reversed;

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
        var rotator = mod.root.Find("Rotator");
        // Currently everything is locked to left-right axis (forward/backward on the gun)
        var direction = mod.parent.InverseTransformDirection(rotator.right);

        // Arbitrary magnitude for the moment
        minLocalPosition = originalLocalPosition - (MOVE_DISTANCE * direction);
        maxLocalPosition = originalLocalPosition + (MOVE_DISTANCE * direction);
        maxDistance = Vector3.Distance(minLocalPosition, maxLocalPosition);
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
        var distance = Vector2.Distance(minScreen, screenPosition);
        if (Settings.StepSize.Value > 0)
        {
            var factor = Mathf.RoundToInt(distance / Settings.StepSize.Value);
            distance = factor * Settings.StepSize.Value;
        }

        float percentDelta = distance / Vector2.Distance(minScreen, maxScreen);

        customizedMod.Move(reversed ?
            Vector3.MoveTowards(maxLocalPosition, minLocalPosition, percentDelta * maxDistance) :
            Vector3.MoveTowards(minLocalPosition, maxLocalPosition, percentDelta * maxDistance));

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
