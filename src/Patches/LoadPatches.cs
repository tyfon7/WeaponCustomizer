using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace WeaponCustomizer;

public static class LoadPatches
{
    public static void Enable()
    {
        new MenuLoadPatch().Enable();
    }

    public class MenuLoadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Class301), nameof(Class301.RequestBuilds));
        }

        [PatchPostfix]
        public static async void Postfix(ISession __instance, Task<IResult> __result)
        {
            await __result;
            Customizations.LoadCustomizations(__instance.Profile.Inventory, __instance.WeaponBuildsStorage).HandleExceptions();
        }
    }
}