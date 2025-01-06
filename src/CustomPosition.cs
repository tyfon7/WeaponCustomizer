using UnityEngine;

namespace WeaponCustomizer;

public struct CustomPosition
{
    public Vector3 OriginalPosition;
    public Vector3 Position;

    public override int GetHashCode()
    {
        int hash = 17;
        hash *= 31 + OriginalPosition.GetHashCode();
        hash *= 31 + Position.GetHashCode();
        return hash;
    }
}