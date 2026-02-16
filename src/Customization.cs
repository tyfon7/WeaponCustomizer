using Newtonsoft.Json;
using UnityEngine;

namespace WeaponCustomizer;

public struct Customization(Vector3 originalPosition, Vector3? position, Quaternion originalRotation, Quaternion? rotation)
{
    public Vector3? OriginalPosition = originalPosition;
    public Vector3? Position = position;

    public Quaternion? OriginalRotation = originalRotation;
    public Quaternion? Rotation = rotation;

    public Customization(Vector3 originalPosition, Quaternion originalRotation) : this(originalPosition, null, originalRotation, null)
    { }

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
    [JsonProperty("position")]
    public Vector3Json? Position;

    [JsonProperty("rotation")]
    public QuaternionJson? Rotation;

    public static implicit operator CustomizationJson(Customization c) => new()
    {
        Position = c.Position,
        Rotation = c.Rotation,
    };

    public static implicit operator Customization(CustomizationJson c) => new()
    {
        Position = c.Position,
        Rotation = c.Rotation,
    };
}

public struct Vector3Json
{
    [JsonProperty("x")]
    public float X;

    [JsonProperty("y")]
    public float Y;

    [JsonProperty("z")]
    public float Z;

    public static implicit operator Vector3Json(Vector3 v) => new() { X = v.x, Y = v.y, Z = v.z };
    public static implicit operator Vector3(Vector3Json v) => new(v.X, v.Y, v.Z);
}

public struct QuaternionJson
{
    [JsonProperty("w")]
    public float W;

    [JsonProperty("x")]
    public float X;

    [JsonProperty("y")]
    public float Y;

    [JsonProperty("z")]
    public float Z;

    public static implicit operator QuaternionJson(Quaternion q) => new() { W = q.w, X = q.x, Y = q.y, Z = q.z };
    public static implicit operator Quaternion(QuaternionJson q) => new(q.X, q.Y, q.Z, q.W);
}