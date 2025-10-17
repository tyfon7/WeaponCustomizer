using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace WeaponCustomizer.Server;

public record SaveRequestData : IRequestData
{
    [JsonPropertyName("data")]
    public CustomizedObject[] Data { get; set; }
}