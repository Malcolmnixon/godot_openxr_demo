using Godot;
using GodotOpenXRDemo.objects;

namespace GodotOpenXRDemo;

public partial class Main : StartVR
{
    private Viewport _vp;

    private Environment _env;

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        _vp = GetViewport();
        _env = GetNode<WorldEnvironment>("World/WorldEnvironment").Environment;

        GetNode<XRButton>("GlowButton").ButtonPressed = _env.GlowEnabled;
        GetNode<XRButton>("MSAAEnabled").ButtonPressed = _vp.Msaa3D != Viewport.Msaa.Disabled;
    }

    public void OnGlowButtonButtonToggled(bool isPressed)
    {
        GD.Print("Glow", isPressed ? "enabled" : "disabled");
        _env.GlowEnabled = isPressed;
    }

    public void OnMsaaButtonButtonToggled(bool isPressed)
    {
        GD.Print("MSAA", isPressed ? "enabled" : "disabled");
        _vp.Msaa3D = isPressed ? Viewport.Msaa.Msaa4X : Viewport.Msaa.Disabled;
    }

    public void OnQuitButtonButtonToggled(bool isPressed)
    {
        GetTree().Quit();
    }
}