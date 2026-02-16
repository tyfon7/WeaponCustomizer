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

    private Image _boneIcon;
    private Transform _mod;
    private CustomizedMod _customizedMod;
    private Weapon _weapon;
    private Slot _slot;
    private CameraViewporter _viewporter;
    private Transform _rotator;
    private Action<bool> _onChange;

    private Vector3 _midLocalPosition;
    private Vector3 _minLocalPosition;
    private Vector3 _maxLocalPosition;
    private Vector2 _minScreen;
    private Vector2 _maxScreen;
    private Vector2 _startScreen;
    private Plane _movementPlane;
    private Vector3 _rotationAxis;
    private Quaternion _rotationStart;

    private bool _dragging;
    private bool _hovered;
    private bool _rotating;

    public void Init(Image boneIcon, Transform mod, Weapon weapon, Slot slot, CameraViewporter viewporter, Action<bool> onChange)
    {
        this._boneIcon = boneIcon;
        this._mod = mod;
        this._weapon = weapon;
        this._slot = slot;
        this._viewporter = viewporter;
        this._onChange = onChange;

        _rotator = mod.root.Find("Rotator");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        SetColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        SetColor();
    }

    private void SetColor()
    {
        _boneIcon.color = _mod.childCount > 0 && (_hovered || _dragging) ? Color.cyan : Color.white;
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
        var customizedMod = _mod.GetComponent<CustomizedMod>();
        if (customizedMod != null)
        {
            customizedMod.Reset();
            Destroy(customizedMod);
            customizedMod = null;

            _weapon.ResetCustomization(_slot);
            _onChange(true);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _mod.childCount <= 0)
        {
            // Cancel drag
            eventData.pointerDrag = null;
            return;
        }

        _dragging = true;
        SetColor();

        _startScreen = eventData.position;

        _customizedMod = _mod.GetComponent<CustomizedMod>();
        if (_customizedMod == null)
        {
            _customizedMod = _mod.gameObject.AddComponent<CustomizedMod>();
            _customizedMod.Init();
        }

        var originalLocalPosition = _customizedMod.OriginalPosition;

        bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        Vector3 upDirection = _mod.parent.InverseTransformDirection(_rotator.up);
        Vector3 forwardDirection = _mod.parent.InverseTransformDirection(_rotator.forward);
        Vector3 rightDirection = _mod.parent.InverseTransformDirection(_rotator.right);

        float distance; // Max distance the mod is movable
        Vector3 direction; // What direction it's moving in
        Vector3 otherOffset; // How the mod has been moved on the *other* two axis

        var offset = _mod.localPosition - originalLocalPosition;
        if (shiftDown)
        {
            direction = upDirection;
            distance = UP_DOWN_MOVE_DISTANCE;
            otherOffset = Vector3.Project(offset, forwardDirection) + Vector3.Project(offset, rightDirection);
            _movementPlane = new Plane(_rotator.forward, _mod.position);
        }
        else if (ctrlDown)
        {
            direction = forwardDirection;
            distance = SIDE_MOVE_DISTANCE;
            otherOffset = Vector3.Project(offset, upDirection) + Vector3.Project(offset, rightDirection);
            _movementPlane = new Plane(_rotator.right, _mod.position);
        }
        else
        {
            direction = rightDirection;
            distance = LEFT_RIGHT_MOVE_DISTANCE;
            otherOffset = Vector3.Project(offset, forwardDirection) + Vector3.Project(offset, upDirection);
            _movementPlane = new Plane(_rotator.forward, _mod.position);
        }

        if (altDown)
        {
            _rotating = true;
            _rotationAxis = _mod.parent.localRotation * direction;
            _rotationStart = _mod.localRotation;
            _minScreen = new(0, eventData.position.y);
            _maxScreen = new(Screen.width, eventData.position.y);
            return;
        }

        _midLocalPosition = originalLocalPosition + otherOffset;
        _minLocalPosition = _midLocalPosition - (distance * direction);
        _maxLocalPosition = _midLocalPosition + (distance * direction);

        Vector3 minPosition = _mod.parent.TransformPoint(_minLocalPosition);
        Vector3 maxPosition = _mod.parent.TransformPoint(_maxLocalPosition);

        _minScreen = _viewporter.TargetCamera.WorldToScreenPoint(minPosition);
        _maxScreen = _viewporter.TargetCamera.WorldToScreenPoint(maxPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rotating)
        {
            OnRotate(eventData);
            return;
        }

        Vector2 mouseVector = eventData.position - _minScreen;
        Vector2 allowedVector = _maxScreen - _minScreen;

        // This gets the amount of a vector A (the mouse position vector) that applies to vector B (the allowed positions of the mod)
        // Which is to say, helps find the point where A projects onto B, aka the closest point on B from the tip of A
        float projectedMagnitude = Vector2.Dot(mouseVector, allowedVector) / allowedVector.magnitude;
        projectedMagnitude = Mathf.Clamp(projectedMagnitude, 0, allowedVector.magnitude);

        Vector2 screenPosition = Vector2.MoveTowards(_minScreen, _maxScreen, projectedMagnitude);

        // With that perfect screen position, raycast onto the weapon plane to find the exact spot where the mod should go
        Ray ray = _viewporter.TargetCamera.ScreenPointToRay(screenPosition);
        if (_movementPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 newLocalPosition = _mod.parent.InverseTransformPoint(hitPoint);

            float moveDistance = (newLocalPosition - _midLocalPosition).magnitude;
            if (Settings.StepSize.Value > 0)
            {
                float localStepSize = Settings.StepSize.Value * STEP_INTERVAL;
                moveDistance = Mathf.RoundToInt(moveDistance / localStepSize) * localStepSize;
            }

            var target = (_maxLocalPosition - newLocalPosition).magnitude > (newLocalPosition - _minLocalPosition).magnitude ?
                _minLocalPosition :
                _maxLocalPosition;

            newLocalPosition = Vector3.MoveTowards(_midLocalPosition, target, moveDistance);
            _customizedMod.Move(newLocalPosition);
        }

        _onChange(false);
    }

    private void OnRotate(PointerEventData eventData)
    {
        float distance = eventData.position.x - _startScreen.x;
        float percent = distance / Screen.width;
        float degrees = 360 * percent;

        if (Settings.RotationStepSize.Value > 0)
        {
            degrees = Mathf.RoundToInt(degrees / Settings.RotationStepSize.Value) * Settings.RotationStepSize.Value;
        }

        _customizedMod.Rotate(_rotationStart * Quaternion.AngleAxis(degrees, _rotationAxis));

        _onChange(false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
        _rotating = false;
        SetColor();

        _weapon.SetCustomization(_slot, _customizedMod.Customization);

        _onChange(true);
    }
}