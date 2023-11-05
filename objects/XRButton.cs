using System.Collections.Generic;
using Godot;

namespace GodotOpenXRDemo.objects;

public partial class XRButton : Node3D
{
    private bool _buttonPressed;

    private bool _timeout;

    private readonly List<Node3D> _bodies = new();

    [Signal]
    public delegate void ButtonToggledEventHandler(bool isPressed);

    [Export]
    public bool ButtonPressed
    {
        get => _buttonPressed;
        set => _SetButtonPressed(value);
    }

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        _UpdateButtonPressed();
    }

    public void OnHandDetectorBodyEntered(Node3D body)
    {
        if (_bodies.Count == 0 && !_timeout)
        {
            _SetButtonPressed(!_buttonPressed);
            EmitSignal(SignalName.ButtonToggled, _buttonPressed);
        }

        if (!_bodies.Contains(body))
            _bodies.Add(body);
    }

    public void OnHandDetectorBodyExited(Node3D body)
    { 
        if (_bodies.Contains(body))
            _bodies.Remove(body);

        if (_bodies.Count == 0 && !_timeout)
        {
            _timeout = true;
            GetNode<Timer>("Timeout").Start();
        }
    }

    public void OnTimeoutTimeout()
    {
        _timeout = false;
    }

    private void _SetButtonPressed(bool isPressed)
    {
        _buttonPressed = isPressed;
        if (IsInsideTree())
            _UpdateButtonPressed();
    }

    private void _UpdateButtonPressed()
    {
        GetNode<Node3D>("OffButton").Visible = !_buttonPressed;
        GetNode<Node3D>("OnButton").Visible = _buttonPressed;
    }
}