using System.Text.Json.Serialization;

namespace WeaponCustomizer.Server;

public record Quaternion
{
    [JsonPropertyName("w")]
    public float W { get; set; }

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("z")]
    public float Z { get; set; }
}