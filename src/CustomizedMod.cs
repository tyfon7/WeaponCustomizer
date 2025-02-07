using UnityEngine;

namespace WeaponCustomizer;

public class CustomizedMod : MonoBehaviour
{
    public CustomPosition Customization { get; private set; }

    public void Init(Vector3 originalPosition, Vector3 position)
    {
        Customization = new(originalPosition, position);
        transform.localPosition = position;
    }

    public void Init(CustomPosition customPosition)
    {
        Customization = customPosition;
        transform.localPosition = customPosition.Position;
    }

    public void Move(Vector3 position)
    {
        Customization = new(Customization.OriginalPosition, position);
        transform.localPosition = position;
    }

    public void LateUpdate()
    {
        transform.localPosition = Customization.Position;
    }

    public void OnDestroy()
    {
        transform.localPosition = Customization.OriginalPosition;
    }
}