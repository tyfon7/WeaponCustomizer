using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using EFT.InventoryLogic;
using EFT.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SPT.Common.Http;

namespace WeaponCustomizer;

public static class Customizations
{
    public static readonly Dictionary<string, Dictionary<string, Customization>> Database = [];

    private static readonly List<CustomizedObject> SaveList = [];

    public static void Save(Weapon weapon, Dictionary<string, Customization> slots)
    {
        Save(weapon.Id, CustomizationType.Weapon, weapon.ShortName.Localized(), slots);
    }

    public static void Save(WeaponBuildClass preset, Dictionary<string, Customization> slots)
    {
        Save(preset.Id, CustomizationType.Preset, preset.HandbookName, slots);
    }

    private static void Save(string id, CustomizationType type, string name, Dictionary<string, Customization> slots)
    {
        CustomizedObject customizedObject = new()
        {
            Id = id,
            Type = type,
            Name = name,
            Slots = []
        };

        if (slots != null)
        {
            foreach (var (slotId, customization) in slots)
            {
                customizedObject.Slots[slotId] = customization;
            }
        }

        SaveList.Add(customizedObject);
        Save();
    }

    private static bool PendingSave = false;

    private static void Save()
    {
        if (!PendingSave)
        {
            PendingSave = true;
            ItemUiContext.Instance.WaitForEndOfFrame(async () =>
            {
                PendingSave = false;
                if (SaveList.Count > 0)
                {
                    try
                    {
                        string json = JsonConvert.SerializeObject(
                            new SaveRequestData() { Data = [.. SaveList] },
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                Converters = { new StringEnumConverter() }
                            });

                        var response = await RequestHandler.PutJsonAsync("/weaponcustomizer/save", json);
                        if (response != "Success")
                        {
                            Plugin.Instance.Logger.LogError("Failed to save. Request: " + json);
                            NotificationManagerClass.DisplayWarningNotification("Failed to save weapon customization - check the server");
                            return;
                        }

                        SaveList.Clear();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Instance.Logger.LogError("Failed to save: " + ex.ToString());
                    }
                }
            });
        }
    }

    public static async Task Load()
    {
        try
        {
            string jsonPayload = await RequestHandler.GetJsonAsync("/weaponcustomizer/load");
            var allCustomizations = JsonConvert.DeserializeObject<Dictionary<string, CustomizedObject>>(jsonPayload);

            foreach (var (id, customizedObject) in allCustomizations)
            {
                var customizations = Database[id] = [];
                foreach (var (slotId, customization) in customizedObject.Slots)
                {
                    customizations[slotId] = customization;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Instance.Logger.LogError("Failed to load: " + ex.ToString());
            NotificationManagerClass.DisplayWarningNotification("Failed to load Weapon Customizations - check the server");
        }
    }

    private struct SaveRequestData
    {
        [JsonProperty("data")]
        public CustomizedObject[] Data;
    }

    private struct CustomizedObject
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public CustomizationType Type;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("slots")]
        public Dictionary<string, CustomizationJson> Slots;
    }

    private enum CustomizationType
    {
        [EnumMember(Value = "unknown")]
        Unknown,
        [EnumMember(Value = "weapon")]
        Weapon,
        [EnumMember(Value = "preset")]
        Preset,
    }
}