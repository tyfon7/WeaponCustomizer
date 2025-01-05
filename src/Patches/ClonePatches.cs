using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace WeaponCustomizer;

public static class ClonePatches
{
    public static void Enable()
    {
        new ClonePatch().Enable();
    }

    public class ClonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass3105), nameof(GClass3105.smethod_2)).MakeGenericMethod([typeof(Item)]);
        }

        [PatchPostfix]
        public static void Postfix(Item originalItem, ref Item __result)
        {
            if (originalItem is not Weapon)
            {
                return;
            }

            Customizations.Copy(originalItem.Id, __result.Id);
        }
    }
}