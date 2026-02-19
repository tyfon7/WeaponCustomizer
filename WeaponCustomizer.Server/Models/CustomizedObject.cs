using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace WeaponCustomizer.Server;

public record CustomizedObject
{
    public enum Type
    {
        [JsonStringEnumMemberName("unknown")]
        Unknown,
        [JsonStringEnumMemberName("weapon")]
        Weapon,
        [JsonStringEnumMemberName("preset")]
        Preset
    }

    [JsonPropertyName("id")]
    public MongoId Id { get; set; }

    [JsonPropertyName("type"), JsonConverter(typeof(JsonStringEnumConverter))]
    public Type CustomizedType { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slots")]
    public Dictionary<string, Customization> Slots { get; set; }
}