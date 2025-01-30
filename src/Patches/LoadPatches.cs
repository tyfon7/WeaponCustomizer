using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System.Reflection;
using System.Threading.Tasks;

namespace WeaponCustomizer;

public static class LoadPatches
{
    public static void Enable()
    {
        new MenuLoadPatch().Enable();
        new OtherInventoryLoadPatch().Enable();
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

    public class OtherInventoryLoadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Init));
        }

        [PatchPostfix]
        public static void Postfix(Profile profile, bool aiControlled)
        {
            // Skip current profile, it was already loaded at menu load
            if (aiControlled || PatchConstants.BackEndSession.Profile.Id == profile.Id)
            {
                return;
            }

            Customizations.LoadCustomizations(profile.Inventory, null).HandleExceptions();
        }
    }
}