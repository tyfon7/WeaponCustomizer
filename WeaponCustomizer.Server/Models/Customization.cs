using System.Text.Json.Serialization;

namespace WeaponCustomizer.Server;

public record Customization
{
    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }

    [JsonPropertyName("rotation")]
    public Quaternion Rotation { get; set; }
}