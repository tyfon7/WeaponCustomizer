using BepInEx;

namespace WeaponCustomizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public void Awake()
    {
        LoadPatches.Enable();
        EditPatches.Enable();
        ClonePatches.Enable();
        ApplyPatches.Enable();
    }
}
