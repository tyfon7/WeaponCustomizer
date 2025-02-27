using UnityEngine;

namespace WeaponCustomizer;

public struct Customization(Vector3 originalPosition, Vector3? position, Quaternion originalRotation, Quaternion? rotation)
{
    public Vector3? OriginalPosition = originalPosition;
    public Vector3? Position = position;

    public Quaternion? OriginalRotation = originalRotation;
    public Quaternion? Rotation = rotation;

    public override int GetHashCode()
    {
        int hash = 17;
        hash *= 31 + OriginalPosition.GetHashCode();
        hash *= 31 + Position.GetHashCode();
        hash *= 31 + OriginalRotation.GetHashCode();
        hash *= 31 + Rotation.GetHashCode();
        return hash;
    }

    public override bool Equals(object other)
    {
        return other is Customization customPosition && this.GetHashCode() == customPosition.GetHashCode();
    }
}

public struct CustomizationJson
{
    public Vector3Json? position;
    public QuaternionJson? rotation;

    public static implicit operator CustomizationJson(Customization c) => new()
    {
        position = c.Position,
        rotation = c.Rotation,
    };

    public static implicit operator Customization(CustomizationJson c) => new()
    {
        Position = c.position,
        Rotation = c.rotation,
    };
}

public struct Vector3Json
{
    public float x;
    public float y;
    public float z;

    public static implicit operator Vector3Json(Vector3 v) => new() { x = v.x, y = v.y, z = v.z };
    public static implicit operator Vector3(Vector3Json v) => new(v.x, v.y, v.z);
}

public struct QuaternionJson
{
    public float w;
    public float x;
    public float y;
    public float z;

    public static implicit operator QuaternionJson(Quaternion q) => new() { w = q.w, x = q.x, y = q.y, z = q.z };
    public static implicit operator Quaternion(QuaternionJson q) => new(q.x, q.y, q.z, q.w);
}