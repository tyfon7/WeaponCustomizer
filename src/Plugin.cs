using BepInEx;

namespace WeaponCustomizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public void Awake()
    {
        EditPatches.Enable();
        ClonePatches.Enable();
        ApplyPatches.Enable();
    }
}
