using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace WeaponCustomizer.Server;

[Injectable(InjectionType = InjectionType.Singleton)]
public class WeaponCustomizer(
    ISptLogger<WeaponCustomizer> logger,
    ModHelper modHelper,
    ProfileHelper profileHelper,
    FileUtil fileUtil,
    JsonUtil jsonUtil)
{
    public Dictionary<MongoId, CustomizedObject> Database { get; set; } = [];

    public async ValueTask<string> SaveCustomizations(SaveRequestData requestData)
    {
        foreach (var customizedObject in requestData.Data)
        {
            if (customizedObject.Slots == null || customizedObject.Slots.Count == 0)
            {
                Database.Remove(customizedObject.Id);
            }
            else
            {
                Database[customizedObject.Id] = customizedObject;
            }
        }

        await Save();

        return "Success";
    }

    public async Task Save()
    {
        var file = new FileFormat()
        {
            Description = "This is a record of all customizations that WeaponCustomizer has made. You can delete this file and restart your server to reset all customizations. Modify this file at your own risk.",
            Version = 2,
            Customizations = Database
        };

        var root = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var filePath = Path.Combine(root, "customizations.json");
        try
        {
            await fileUtil.WriteFileAsync(filePath, jsonUtil.Serialize(file, true));
        }
        catch (Exception ex)
        {
            logger.Error("WeaponCustomizer: Failed to save customizations!", ex);
        }
    }

    public async Task Load()
    {
        var root = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var filePath = Path.Combine(root, "customizations.json");

        try
        {
            if (!fileUtil.FileExists(filePath))
            {
                Database = [];
                await Save();
                return;
            }

            var json = await fileUtil.ReadFileAsync(filePath);
            var file = jsonUtil.Deserialize<FileFormat>(json);

            switch (file.Version)
            {
                case 2:
                    Database = file.Customizations;
                    break;
                default:
                    throw new Exception("Unknown file version");
            }

            Database = file.Customizations;
        }
        catch (Exception ex)
        {
            logger.Error("WeaponCustomizer: Failed to load customizations!", ex);
            Database = [];
        }
    }

    public async Task Clean()
    {
        Dictionary<MongoId, bool> customizedItems = [];
        foreach (var id in Database.Keys)
        {
            customizedItems[id] = false;
        }

        foreach (var (profileId, profile) in profileHelper.GetProfiles())
        {
            foreach (var item in profile.CharacterData?.PmcData?.Inventory?.Items ?? [])
            {
                if (customizedItems.ContainsKey(item.Id))
                {
                    customizedItems[item.Id] = true;
                }
            }

            foreach (var preset in profile.UserBuildData?.WeaponBuilds ?? [])
            {
                if (customizedItems.ContainsKey(preset.Id))
                {
                    customizedItems[preset.Id] = true;
                }
            }

            foreach (var insurance in profile.InsuranceList ?? [])
            {
                foreach (var item in insurance.Items ?? [])
                {
                    if (customizedItems.ContainsKey(item.Id))
                    {
                        customizedItems[item.Id] = true;
                    }
                }
            }
        }

        int dirtyCount = 0;
        foreach (var (id, found) in customizedItems)
        {
            if (!found)
            {
                Database.Remove(id);
                dirtyCount++;
            }
        }

        if (dirtyCount > 0)
        {
            logger.Debug($"WeaponCustomizer: Cleaned up {dirtyCount} customizations for weapons/presets that no longer exist");
            await Save();
        }
    }
}