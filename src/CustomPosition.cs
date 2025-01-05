using UnityEngine;

namespace WeaponCustomizer;

public struct CustomPosition
{
    public Vector3 OriginalPosition;
    public Vector3 Position;

    public CustomPosition(Vector3 originalPosition, Vector3 position)
    {
        OriginalPosition = originalPosition;
        Position = position;
    }
}