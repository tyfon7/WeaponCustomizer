using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace WeaponCustomizer.Server;

public record FileFormat
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("customizations")]
    public Dictionary<MongoId, CustomizedObject> Customizations { get; set; }
}