using System.Collections.Generic;
using System.ComponentModel;
using BepInEx.Configuration;

namespace WeaponCustomizer;

internal enum ModRaidWeapon
{
    Never,
    [Description("With Multitool")]
    WithTool,
    Always
}

internal class Settings
{
    private const string GeneralSection = "General";

    public static ConfigEntry<int> StepSize { get; set; }
    public static ConfigEntry<int> RotationStepSize { get; set; }
    public static ConfigEntry<bool> MoveEverything { get; set; }
    public static ConfigEntry<ModRaidWeapon> ModifyRaidWeapons { get; set; }

    public static void Init(ConfigFile config)
    {
        var configEntries = new List<ConfigEntryBase>();

        configEntries.Add(ModifyRaidWeapons = config.Bind(
            GeneralSection,
            "Customize Weapons In Raid",
            ModRaidWeapon.Never,
            new ConfigDescription(
                "When to enable the customization of weapons in raid",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(StepSize = config.Bind(
            GeneralSection,
            "Step Size",
            0,
            new ConfigDescription(
                "Moves attachments in discrete step intervals. Use 0 for smooth motion.",
                new AcceptableValueRange<int>(0, 20),
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RotationStepSize = config.Bind(
            GeneralSection,
            "Rotation Angle",
            0,
            new ConfigDescription(
                "Rotate attachments in steps by degrees. Use 0 for smooth rotation.",
                new AcceptableValueRange<int>(0, 90),
                new ConfigurationManagerAttributes { })));

        configEntries.Add(MoveEverything = config.Bind(
            GeneralSection,
            "Move Everything",
            false,
            new ConfigDescription(
                "Allow every part of the gun to be moved. Doesn't make sense, is silly, have fun.",
                null,
                new ConfigurationManagerAttributes { })));

        RecalcOrder(configEntries);
    }

    private static void RecalcOrder(List<ConfigEntryBase> configEntries)
    {
        // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
        int settingOrder = configEntries.Count;
        foreach (var entry in configEntries)
        {
            if (entry.Description.Tags[0] is ConfigurationManagerAttributes attributes)
            {
                attributes.Order = settingOrder;
            }

            settingOrder--;
        }
    }
}