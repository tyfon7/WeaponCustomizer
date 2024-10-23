using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using TMPro;
using UnityEngine.EventSystems;

namespace WeaponCustomizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public void Awake()
    {
        EditPatches.Enable();
    }
}
