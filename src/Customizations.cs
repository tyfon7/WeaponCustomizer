using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace WeaponCustomizer;

public static class Customizations
{
    public static readonly Dictionary<string, Dictionary<string, Customization>> Database = [];

    public static void Save(Weapon weapon, Dictionary<string, Customization> slots)
    {
        Save(weapon.Id, CustomizationType.Weapon, weapon.ShortName.Localized(), slots);
    }

    public static void Save(Preset preset, Dictionary<string, Customization> slots)
    {
        Save(preset.Id, CustomizationType.Preset, preset.HandbookName, slots);
    }

    private static void Save(string id, CustomizationType type, string name, Dictionary<string, Customization> slots)
    {
        CustomizedObject payload = new()
        {
            id = id,
            type = type,
            name = name,
            slots = []
        };

        if (slots != null)
        {
            foreach (var (slotId, customPosition) in slots)
            {
                payload.slots[slotId] = customPosition;
            }
        }

        try
        {
            string json = JsonConvert.SerializeObject(
                payload,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            RequestHandler.PutJsonAsync("/weaponcustomizer/save", json);
        }
        catch (Exception ex)
        {
            Plugin.Instance.Logger.LogError("Failed to save: " + ex.ToString());
            NotificationManagerClass.DisplayWarningNotification("Failed to save weapon customization - check the server");
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
                foreach (var (slotId, customization) in customizedObject.slots)
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

    private struct CustomizedObject
    {
        public string id;
        public CustomizationType type;
        public string name;
        public Dictionary<string, CustomizationJson> slots;
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