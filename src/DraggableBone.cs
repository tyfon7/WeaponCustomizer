using System;
using EFT.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WeaponCustomizer;

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

    public void Init(Transform mod, string weaponId, string slotId, CameraViewporter viewporter, Action updatePositions)
    {
        this.mod = mod;
        this.weaponId = weaponId;
        this.slotId = slotId;
        this.viewporter = viewporter;
        this.updatePositions = updatePositions;

        originalLocalPosition = mod.localPosition;
        if (Customizations.IsCustomized(weaponId, slotId, out CustomPosition customPosition))
        {
            originalLocalPosition = customPosition.OriginalPosition;
        }

        // Currently everything is locked to left-right axis (forward/backward on the gun)
        var direction = mod.parent.InverseTransformDirection(Vector3.right);

        // Arbitrary magnitude for the moment
        minLocalPosition = originalLocalPosition + (-0.2f * direction);
        maxLocalPosition = originalLocalPosition + (0.2f * direction);
        maxDistance = Vector3.Distance(minLocalPosition, maxLocalPosition);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            mod.localPosition = originalLocalPosition;

            updatePositions();

            Customizations.Reset(weaponId, slotId);
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
        Customizations.Set(weaponId, slotId, new(originalLocalPosition, mod.localPosition));
    }
}
