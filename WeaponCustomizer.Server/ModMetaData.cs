using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace WeaponCustomizer.Server;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.tyfon.weaponcustomizer";
    public string Name { get; init; } = "Weapon Customizer";

    public string Author { get; init; } = "Tyfon";

    public List<string> Contributors { get; init; }

    public SemanticVersioning.Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version.ToString(3));

    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");

    public List<string> Incompatibilities { get; init; }

    public Dictionary<string, SemanticVersioning.Range> ModDependencies { get; init; }

    public string Url { get; init; } = "https://github.com/tyfon7/weaponcustomizer";

    public bool HasPrepatcher { get; init; } = false;

    public string License { get; init; } = "MIT";
}