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
            return AccessTools.Method(typeof(Class1374), nameof(Class1374.SelectProfile));
        }

        [PatchPostfix]
        public static async void Postfix(ISession backendSession, Task __result)
        {
            await __result;
            Customizations.LoadCustomizations(backendSession.Profile.Inventory).HandleExceptions();
        }
    }
}