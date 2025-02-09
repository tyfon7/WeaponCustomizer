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

public struct CustomPositionJson
{
    public Vector3Json position;
    public Vector3Json original;

    public static implicit operator CustomPositionJson(CustomPosition c) => new() { position = c.Position, original = c.OriginalPosition };
    public static implicit operator CustomPosition(CustomPositionJson c) => new() { OriginalPosition = c.original, Position = c.position };
}

public struct Vector3Json
{
    public float x;
    public float y;
    public float z;

    public static implicit operator Vector3Json(Vector3 v) => new() { x = v.x, y = v.y, z = v.z };
    public static implicit operator Vector3(Vector3Json v) => new(v.x, v.y, v.z);
}