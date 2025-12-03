using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;

namespace WeaponCustomizer;

[BepInPlugin("com.tyfon.weaponcustomizer", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;

    public new ManualLogSource Logger => base.Logger;

    public void Awake()
    {
        Instance = this;

        Settings.Init(Config);

        LoadPatches.Enable();
        EditPatches.Enable();
        ClonePatches.Enable();
        ApplyPatches.Enable();
    }

    public static bool InRaid()
    {
        var instance = Singleton<AbstractGame>.Instance;
        return instance != null && instance.InRaid;
    }
}
