using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace WeaponCustomizer.Server;

public class ReplaceIdsPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemExtensions), nameof(ItemExtensions.ReplaceIDs));
    }

    [PatchPrefix]
    public static void Prefix(IEnumerable<Item> items, ref IEnumerable<Item> __state)
    {
        var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>();

        __state = cloner.Clone(items);
    }

    [PatchPostfix]
    public static void Postfix(IEnumerable<Item> __state, IEnumerable<Item> __result)
    {
        var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>();
        var weaponCustomizer = ServiceLocator.ServiceProvider.GetService<WeaponCustomizer>();
        var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>();

        bool dirty = false;
        foreach (var (originalItem, newItem) in __state.Zip(__result))
        {
            if (weaponCustomizer.Database.TryGetValue(originalItem.Id, out CustomizedObject customizedObject))
            {
                weaponCustomizer.Database[newItem.Id] = cloner.Clone(customizedObject);
                dirty = true;

                logger.Debug($"WeaponCustomizer: weapon {originalItem.Id} is now {newItem.Id}, customizations copied");
            }
        }

        if (dirty)
        {
            // Fire and forget
            var task = weaponCustomizer.Save();
        }
    }
}
