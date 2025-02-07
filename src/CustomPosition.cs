using UnityEngine;

namespace WeaponCustomizer;

public struct CustomPosition(Vector3 originalPosition, Vector3 position)
{
    public Vector3 OriginalPosition = originalPosition;
    public Vector3 Position = position;

    public override int GetHashCode()
    {
        int hash = 17;
        hash *= 31 + OriginalPosition.GetHashCode();
        hash *= 31 + Position.GetHashCode();
        return hash;
    }

    public override bool Equals(object other)
    {
        return other is CustomPosition customPosition && this.GetHashCode() == customPosition.GetHashCode();
    }
}