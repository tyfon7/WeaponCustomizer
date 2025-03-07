using System;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeaponCustomizer;

public class DraggableBone : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float LEFT_RIGHT_MOVE_DISTANCE = 0.5f;
    private const float UP_DOWN_MOVE_DISTANCE = 0.2f;
    private const float SIDE_MOVE_DISTANCE = 0.2f;
    private const float STEP_INTERVAL = 0.002f;

    private Image boneIcon;
    private Transform mod;
    private CustomizedMod customizedMod;
    private Weapon weapon;
    private string slotId;
    private CameraViewporter viewporter;
    private Transform rotator;
    private Action<bool> onChange;

    private Vector3 midLocalPosition;
    private Vector3 minLocalPosition;
    private Vector3 maxLocalPosition;
    private Vector2 minScreen;
    private Vector2 maxScreen;
    private Vector2 startScreen;
    private Plane movementPlane;
    private Vector3 rotationAxis;
    private Quaternion rotationStart;

    private bool dragging;
    private bool hovered;
    private bool rotating;

    public void Init(Image boneIcon, Transform mod, Weapon weapon, string slotId, CameraViewporter viewporter, Action<bool> onChange)
    {
        this.boneIcon = boneIcon;
        this.mod = mod;
        this.weapon = weapon;
        this.slotId = slotId;
        this.viewporter = viewporter;
        this.onChange = onChange;

        rotator = mod.root.Find("Rotator");
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

            weapon.ResetCustomization(slotId);
            onChange(true);
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

        dragging = true;
        SetColor();

        startScreen = eventData.position;

        customizedMod = mod.GetComponent<CustomizedMod>();
        if (customizedMod == null)
        {
            customizedMod = mod.gameObject.AddComponent<CustomizedMod>();
            customizedMod.Init();
        }

        var originalLocalPosition = customizedMod.OriginalPosition;

        bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        Vector3 upDirection = mod.parent.InverseTransformDirection(rotator.up);
        Vector3 forwardDirection = mod.parent.InverseTransformDirection(rotator.forward);
        Vector3 rightDirection = mod.parent.InverseTransformDirection(rotator.right);

        float distance; // Max distance the mod is movable
        Vector3 direction; // What direction it's moving in
        Vector3 otherOffset; // How the mod has been moved on the *other* two axis

        var offset = mod.localPosition - originalLocalPosition;
        if (shiftDown)
        {
            direction = upDirection;
            distance = UP_DOWN_MOVE_DISTANCE;
            otherOffset = Vector3.Project(offset, forwardDirection) + Vector3.Project(offset, rightDirection);
            movementPlane = new Plane(rotator.forward, mod.position);
        }
        else if (ctrlDown)
        {
            direction = forwardDirection;
            distance = SIDE_MOVE_DISTANCE;
            otherOffset = Vector3.Project(offset, upDirection) + Vector3.Project(offset, rightDirection);
            movementPlane = new Plane(rotator.right, mod.position);
        }
        else
        {
            direction = rightDirection;
            distance = LEFT_RIGHT_MOVE_DISTANCE;
            otherOffset = Vector3.Project(offset, forwardDirection) + Vector3.Project(offset, upDirection);
            movementPlane = new Plane(rotator.forward, mod.position);
        }

        if (altDown)
        {
            rotating = true;
            rotationAxis = mod.parent.localRotation * direction;
            rotationStart = mod.localRotation;
            minScreen = new(0, eventData.position.y);
            maxScreen = new(Screen.width, eventData.position.y);
            return;
        }

        midLocalPosition = originalLocalPosition + otherOffset;
        minLocalPosition = midLocalPosition - (distance * direction);
        maxLocalPosition = midLocalPosition + (distance * direction);

        Vector3 minPosition = mod.parent.TransformPoint(minLocalPosition);
        Vector3 maxPosition = mod.parent.TransformPoint(maxLocalPosition);

        minScreen = viewporter.TargetCamera.WorldToScreenPoint(minPosition);
        maxScreen = viewporter.TargetCamera.WorldToScreenPoint(maxPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rotating)
        {
            OnRotate(eventData);
            return;
        }

        Vector2 mouseVector = eventData.position - minScreen;
        Vector2 allowedVector = maxScreen - minScreen;

        // This gets the amount of a vector A (the mouse position vector) that applies to vector B (the allowed positions of the mod)
        // Which is to say, helps find the point where A projects onto B, aka the closest point on B from the tip of A
        float projectedMagnitude = Vector2.Dot(mouseVector, allowedVector) / allowedVector.magnitude;
        projectedMagnitude = Mathf.Clamp(projectedMagnitude, 0, allowedVector.magnitude);

        Vector2 screenPosition = Vector2.MoveTowards(minScreen, maxScreen, projectedMagnitude);

        // With that perfect screen position, raycast onto the weapon plane to find the exact spot where the mod should go
        Ray ray = viewporter.TargetCamera.ScreenPointToRay(screenPosition);
        if (movementPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 newLocalPosition = mod.parent.InverseTransformPoint(hitPoint);

            float moveDistance = (newLocalPosition - midLocalPosition).magnitude;
            if (Settings.StepSize.Value > 0)
            {
                float localStepSize = Settings.StepSize.Value * STEP_INTERVAL;
                moveDistance = Mathf.RoundToInt(moveDistance / localStepSize) * localStepSize;
            }

            var target = (maxLocalPosition - newLocalPosition).magnitude > (newLocalPosition - minLocalPosition).magnitude ?
                minLocalPosition :
                maxLocalPosition;

            newLocalPosition = Vector3.MoveTowards(midLocalPosition, target, moveDistance);
            customizedMod.Move(newLocalPosition);
        }

        onChange(false);
    }

    private void OnRotate(PointerEventData eventData)
    {
        float distance = eventData.position.x - startScreen.x;
        float percent = distance / Screen.width;
        float degrees = 360 * percent;

        if (Settings.RotationStepSize.Value > 0)
        {
            degrees = Mathf.RoundToInt(degrees / Settings.RotationStepSize.Value) * Settings.RotationStepSize.Value;
        }

        customizedMod.Rotate(rotationStart * Quaternion.AngleAxis(degrees, rotationAxis));

        onChange(false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        rotating = false;
        SetColor();

        weapon.SetCustomization(slotId, customizedMod.Customization);

        onChange(true);
    }
}
