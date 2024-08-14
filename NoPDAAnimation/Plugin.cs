using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace NoPDAAnimation;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.snmodding.nautilus")]
public class Plugin : BaseUnityPlugin
{
    public new static ManualLogSource Logger { get; private set; }
    public static ModConfig ModConfig = OptionsPanelHandler.RegisterModOptions<ModConfig>();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
    private void Awake()
    {
        // set project-scoped logger instance
        Logger = base.Logger;

        // register harmony patches
        Harmony harmony = new($"{PluginInfo.PLUGIN_GUID}");
        if (ModConfig.RemovePDAAnimation)
        {
            harmony.PatchAll(typeof(Patch_Animation.Patch_ArmsController_SetUsingPda));
        }
        if (ModConfig.RemoveCameraMove)
        {
            harmony.PatchAll(typeof(Patch_CameraMove.Patch_MainCameraControl_OnUpdate));
        }
        if (ModConfig.RemoveFOVChange)
        {
            harmony.PatchAll(typeof(Patch_FOVChange.Patch_PDACameraFOVControl_Update));
        }
        if (ModConfig.RemoveResetCameraHorizontalView)
        {
            harmony.PatchAll(typeof(Patch_ResetCameraHorizontalView.Patch_MainCameraControl_OnUpdate));
        }

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
}
