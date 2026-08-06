using System.Reflection;
using System.Threading.Tasks;
using ChatShared;
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
            return AccessTools.Method(typeof(EftClientBackendSession), nameof(EftClientBackendSession.RequestBuilds));
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
            return AccessTools.Method(typeof(SocialNetwork), nameof(SocialNetwork.DisplayMessage), [typeof(DialogueChatMessage), typeof(string)]);
        }

        [PatchPostfix]
        public static void Postfix(DialogueChatMessage message)
        {
            if (message.HasRewards && message.Type == EMessageType.InsuranceReturn)
            {
                Customizations.Load().HandleExceptions();
            }
        }
    }
}