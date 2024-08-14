using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace NoPDAAnimation;

[Menu(PluginInfo.PLUGIN_NAME)]
public class ModConfig : ConfigFile
{
    public const string STR_RESTART_REQUIRED = "Game restart required to take effect.";
    public const string STR_PDA_POS = "PDA position if animation is removed.";
    public const string FORMAT = "{0:F3}";

    [Toggle("Remove animation", Tooltip = "Remove animation when opening PDA. " + STR_RESTART_REQUIRED)]
    public bool RemovePDAAnimation = true;

    [Toggle("Remove camera movement", Tooltip = "Remove camera movement when opening PDA. " + STR_RESTART_REQUIRED)]
    public bool RemoveCameraMove = true;

    [Toggle("Remove FOV change", Tooltip = "Remove field of view change when opening PDA. " + STR_RESTART_REQUIRED)]
    public bool RemoveFOVChange = true;

    [Toggle("Remove camera reset", Tooltip = "Remove horizontal view camera reset when opening PDA. " + STR_RESTART_REQUIRED)]
    public bool RemoveResetCameraHorizontalView = false;

    [Slider("PDA X position", -0.3f, -0.1f,
        Tooltip = "Mod default: -0.2 " + STR_PDA_POS, Step = 0.005f, Format = FORMAT),
        OnChange(nameof(OnChangeXPos))]
    public float X = -0.2f;

    [Slider("PDA Y position", -0.01f, 0.01f,
        Tooltip = "Mod default: 0 " + STR_PDA_POS, Step = 0.001f, Format = FORMAT),
        OnChange(nameof(OnChangeYPos))]
    public float Y = 0f;

    [Slider("PDA Z position", 0.09f, 0.2f,
        Tooltip = "Mod default: 0.11 " + STR_PDA_POS, Step = 0.001f, Format = FORMAT),
        OnChange(nameof(OnChangeZPos))]
    public float Z = 0.11f;

    private void OnChangeXPos(SliderChangedEventArgs e)
    {
        PDA pda = Player.main.GetPDA();
        Vector3 pos = pda.transform.localPosition;

        if (pda != null)
        {
            pda.transform.localPosition = new Vector3(e.Value, pos.y, pos.z);
        }
    }

    private void OnChangeYPos(SliderChangedEventArgs e)
    {
        PDA pda = Player.main.GetPDA();
        Vector3 pos = pda.transform.localPosition;

        if (pda != null)
        {
            pda.transform.localPosition = new Vector3(pos.x, e.Value, pos.z);
        }
    }

    private void OnChangeZPos(SliderChangedEventArgs e)
    {
        PDA pda = Player.main.GetPDA();
        Vector3 pos = pda.transform.localPosition;

        if (pda != null)
        {
            pda.transform.localPosition = new Vector3(pos.x, pos.y, e.Value);
        }
    }
}
