using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace WeaponCustomizer.Server;

public class ReplaceIdsPatch : AbstractPatch
{
    private static WeaponCustomizer WeaponCustomizer;
    private static ISptLogger<ReplaceIdsPatch> Logger;
    private static ICloner Cloner;

    public ReplaceIdsPatch(WeaponCustomizer weaponCustomizer, ISptLogger<ReplaceIdsPatch> logger, ICloner cloner)
    {
        WeaponCustomizer = weaponCustomizer;
        Logger = logger;
        Cloner = cloner;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemExtensions), nameof(ItemExtensions.ReplaceIDs));
    }

    [PatchPrefix]
    public static void Prefix(IEnumerable<Item> items, ref IEnumerable<Item> __state)
    {
        __state = Cloner.Clone(items);
    }

    [PatchPostfix]
    public static void Postfix(IEnumerable<Item> __state, IEnumerable<Item> __result)
    {
        bool dirty = false;
        foreach (var (originalItem, newItem) in __state.Zip(__result))
        {
            if (WeaponCustomizer.Database.TryGetValue(originalItem.Id, out CustomizedObject customizedObject))
            {
                WeaponCustomizer.Database[newItem.Id] = Cloner.Clone(customizedObject);
                dirty = true;

                Logger.Debug($"WeaponCustomizer: weapon {originalItem.Id} is now {newItem.Id}, customizations copied");
            }
        }

        if (dirty)
        {
            // Fire and forget
            _ = WeaponCustomizer.Save();
        }
    }
}