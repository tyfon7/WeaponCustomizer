using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace WeaponCustomizer;

public static class LoadPatches
{
    public static void Enable()
    {
        new MenuLoadPatch().Enable();
        new OtherInventoryLoadPatch().Enable();
        new InsuranceMessageReceivedPatch().Enable();
    }

    public class MenuLoadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(
                t => !t.IsAbstract &&
                typeof(ProfileEndpointFactoryAbstractClass).IsAssignableFrom(t) &&
                t.GetMethod("RequestBuilds") != null);
            return AccessTools.Method(type, "RequestBuilds");
        }

        [PatchPostfix]
        public static async void Postfix(Task<IResult> __result)
        {
            await __result;
            Customizations.Load().HandleExceptions();
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

            Customizations.Load().HandleExceptions();
        }
    }

    // Reload customizations after insurance return, because some of the items might have changed IDs
    public class InsuranceMessageReceivedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SocialNetworkClass), nameof(SocialNetworkClass.method_7));
        }

        [PatchPostfix]
        public static void Postfix(ChatMessageClass message)
        {
            if (message.HasRewards && message.Type == ChatShared.EMessageType.InsuranceReturn)
            {
                Customizations.Load().HandleExceptions();
            }
        }
    }
}