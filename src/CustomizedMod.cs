using UnityEngine;

namespace WeaponCustomizer;

public class CustomizedMod : MonoBehaviour
{
    public Customization Customization { get; private set; }

    public Vector3 Position => Customization.Position.Value;
    public Vector3 OriginalPosition => Customization.OriginalPosition.Value;
    public Quaternion Rotation => Customization.Rotation.Value;
    public Quaternion OriginalRotation => Customization.OriginalRotation.Value;

    public void Init()
    {
        Customization = new(transform.localPosition, transform.localRotation);
    }

    public void Init(Customization customization)
    {
        Customization = new(
            customization.OriginalPosition ?? transform.localPosition,
            customization.Position,
            customization.OriginalRotation ?? transform.localRotation,
            customization.Rotation
        );

        if (customization.Position.HasValue)
        {
            transform.localPosition = customization.Position.Value;
        }

        if (customization.Rotation.HasValue)
        {
            transform.localRotation = customization.Rotation.Value;
        }
    }

    public void Move(Vector3 position)
    {
        Customization = new(OriginalPosition, position, OriginalRotation, Customization.Rotation);
        transform.localPosition = position;
    }

    public void Rotate(Quaternion rotation)
    {
        Customization = new(OriginalPosition, Customization.Position, OriginalRotation, rotation);
        transform.localRotation = rotation;
    }

    public void Reset()
    {
        Move(OriginalPosition);
        Rotate(OriginalRotation);
    }

    public void LateUpdate()
    {
        if (Customization.Position.HasValue)
        {
            transform.localPosition = Customization.Position.Value;
        }

        if (Customization.Rotation.HasValue)
        {
            transform.localRotation = Customization.Rotation.Value;
        }
    }

    public void OnDestroy()
    {
        transform.localPosition = OriginalPosition;
        transform.localRotation = OriginalRotation;
    }
}