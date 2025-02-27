using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace WeaponCustomizer;

public static class Customizations
{
    public static readonly Dictionary<string, Dictionary<string, Customization>> Database = [];

    public static void Save(string id, Dictionary<string, Customization> slots)
    {
        SavePayload payload = new()
        {
            id = id,
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
            string payload = await RequestHandler.GetJsonAsync("/weaponcustomizer/load");
            var allCustomizations = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CustomizationJson>>>(payload);

            foreach (var (id, slots) in allCustomizations)
            {
                var customizations = Database[id] = [];
                foreach (var (slotId, customization) in slots)
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

    private struct SavePayload
    {
        public string id;
        public Dictionary<string, CustomizationJson> slots;
    }
}