using BepInEx;
using BepInEx.Logging;

namespace WeaponCustomizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;

    public new ManualLogSource Logger => base.Logger;

    public void Awake()
    {
        Instance = this;

        LoadPatches.Enable();
        EditPatches.Enable();
        ClonePatches.Enable();
        ApplyPatches.Enable();
    }
}
