using Godot;

namespace GodotOpenXRDemo;

public partial class StartVR : Node3D
{
    [Signal]
    public delegate void FocusLostEventHandler();

    [Signal]
    public delegate void FocusGainedEventHandler();

    [Signal]
    public delegate void PoseRecenteredEventHandler();

    [Export]
    public int MaximumRefreshRate { get; set; } = 90;

    private OpenXRInterface _xrInterface;

    private bool _xrIsFocused;
    
    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        _xrInterface = (OpenXRInterface)XRServer.FindInterface("OpenXR");
        if (_xrInterface != null && _xrInterface.IsInitialized())
        {
            GD.Print("OpenXR instantiated successfully.");
            var vp = GetViewport();

            // Enable XR on our viewport
            vp.UseXR = true;

            // Make sure v-sync is off, v-sync is handled by OpenXR
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

            // Enable VRS
            if (RenderingServer.GetRenderingDevice() != null)
                vp.VrsMode = Viewport.VrsModeEnum.XR;
            else if ((int)ProjectSettings.GetSetting("xr/openxr/foveation_level") == 0)
                GD.PushWarning("OpenXR: Recommend setting Foveation level to High in Project Settings");

            // Connect the OpenXR events
            _xrInterface.SessionBegun += OnOpenXRSessionBegun;
            _xrInterface.SessionVisible += OnOpenXRVisibleState;
            _xrInterface.SessionFocussed += OnOpenXRFocusedState;
            _xrInterface.SessionStopping += OnOpenXRStopping;
            _xrInterface.PoseRecentered += OnOpenXRPoseRecentered;
        }
        else
        {
            // We couldn't start OpenXR.
            GD.Print("OpenXR not instantiated!");
            GetTree().Quit();
        }
    }

    /// <summary>
    /// Handle OpenXR session ready
    /// </summary>
    private void OnOpenXRSessionBegun()
    {
        // Get the reported refresh rate
        var currentRefreshRate = _xrInterface.DisplayRefreshRate;
        GD.Print(currentRefreshRate > 0.0F
            ? $"OpenXR: Refresh rate reported as {currentRefreshRate}"
            : "OpenXR: No refresh rate given by XR runtime");

        // See if we have a better refresh rate available
        var newRate = currentRefreshRate;
        var availableRates = _xrInterface.GetAvailableDisplayRefreshRates();
        if (availableRates.Count == 0)
        {
            GD.Print("OpenXR: Target does not support refresh rate extension");
        }
        else if (availableRates.Count == 1)
        {
            // Only one available, so use it
            newRate = (float)availableRates[0];
        }
        else
        {
            GD.Print("OpenXR: Available refresh rates: ", availableRates);
            foreach (float rate in availableRates)
                if (rate > newRate && rate <= MaximumRefreshRate)
                    newRate = rate;
        }

        // Did we find a better rate?
        if (currentRefreshRate != newRate)
        {
            GD.Print($"OpenXR: Setting refresh rate to {newRate}");
            _xrInterface.DisplayRefreshRate = newRate;
            currentRefreshRate = newRate;
        }

        // Now match our physics rate
        Engine.PhysicsTicksPerSecond = (int)currentRefreshRate;
    }

    /// <summary>
    /// Handle OpenXR visible state
    /// </summary>
    private void OnOpenXRVisibleState()
    {
        // We always pass this state at startup,
        // but the second time we get this it means our player took off their headset
        if (_xrIsFocused)
        {
            GD.Print("OpenXR lost focus");

            _xrIsFocused = false;

            // Pause our game
            ProcessMode = ProcessModeEnum.Disabled;

            EmitSignal(SignalName.FocusLost);
        }
    }

    /// <summary>
    /// Handle OpenXR focused state
    /// </summary>
    private void OnOpenXRFocusedState()
    {
        GD.Print("OpenXR gained focus");
        _xrIsFocused = true;

        // Un-pause our game
        ProcessMode = ProcessModeEnum.Inherit;

        EmitSignal(SignalName.FocusGained);
    }

    /// <summary>
    /// Handle OpenXR stopping state
    /// </summary>
    private void OnOpenXRStopping()
    {
        // Our session is being stopped.
        GD.Print("OpenXR is stopping");
    }

    /// <summary>
    /// Handle OpenXR pose recentered signal
    /// </summary>
    private void OnOpenXRPoseRecentered()
    {
        // User recentered view, we have to react to this by recentering the view.
        // This is game implementation dependent.
        EmitSignal(SignalName.PoseRecentered);
    }
}