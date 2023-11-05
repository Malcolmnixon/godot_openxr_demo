using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace GodotOpenXRDemo;

public partial class XRAction : Node
{
    /// <summary>
    /// Action Listener for positional tracker events
    /// </summary>
    private sealed class XRActionListener
    {
        /// <summary>
        /// Initializes a new instance of the XRActionListener class.
        /// </summary>
        /// <param name="action">Owning XRAction</param>
        /// <param name="tracker">Tracker to listen to</param>
        public XRActionListener(XRAction action, XRPositionalTracker tracker)
        {
            Action = action;
            Tracker = tracker;
        }

        /// <summary>
        /// Parent XRAction
        /// </summary>
        private XRAction Action { get; }

        /// <summary>
        /// Tracker
        /// </summary>
        public XRPositionalTracker Tracker { get; }

        /// <summary>
        /// Called when a button is pressed on the tracker
        /// </summary>
        /// <param name="actionName">Action name</param>
        public void OnButtonPressed(string actionName)
        {
            if (actionName == Action.Action)
                Action.EmitSignal(SignalName.ButtonPressed, Tracker);
        }

        /// <summary>
        /// Called when a button is released on the tracker
        /// </summary>
        /// <param name="actionName">Action name</param>
        public void OnButtonReleased(string actionName)
        {
            if (actionName == Action.Action)
                Action.EmitSignal(SignalName.ButtonReleased, Tracker);
        }

        /// <summary>
        /// Called when a float input is changed on the tracker
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="value">Float value</param>
        public void OnInputFloatChanged(string actionName, double value)
        {
            if (actionName == Action.Action)
                Action.EmitSignal(SignalName.InputFloatChanged, value, Tracker);
        }

        /// <summary>
        /// Called when a vector2 input is changed on the tracker
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="value">Vector2 value</param>
        public void OnInputVector2Changed(string actionName, Vector2 value)
        {
            if (actionName == Action.Action)
                Action.EmitSignal(SignalName.InputVector2Changed, value, Tracker);
        }

        /// <summary>
        /// Called when a pose changes on the tracker
        /// </summary>
        /// <param name="pose">Pose</param>
        public void OnPoseChanged(XRPose pose)
        {
            if (pose.Name == Action.Action)
                Action.EmitSignal(SignalName.PoseChanged, pose, Tracker);
        }

        /// <summary>
        /// Called when a pose loses tracking on the tracker
        /// </summary>
        /// <param name="pose">Pose</param>
        public void OnPoseLostTracking(XRPose pose)
        {
            if (pose.Name == Action.Action)
                Action.EmitSignal(SignalName.PoseLostTracking, pose, Tracker);
        }
    }

    /// <summary>
    /// Data type
    /// </summary>
    public enum DataType
    {
        Bool,
        Float,
        Vector2,
        Pose
    }

    /// <summary>
    /// Dictionary of tracker listeners
    /// </summary>
    private readonly Dictionary<StringName, XRActionListener> _listeners = new();

    /// <summary>
    /// Type of data being listened for
    /// </summary>
    private DataType _type = DataType.Bool;

    /// <summary>
    /// Button pressed signal
    /// </summary>
    /// <param name="tracker">Tracker that pressed the button action</param>
    [Signal]
    public delegate void ButtonPressedEventHandler(XRPositionalTracker tracker);

    /// <summary>
    /// Button released signal
    /// </summary>
    /// <param name="tracker">Tracker that released the button action</param>
    [Signal]
    public delegate void ButtonReleasedEventHandler(XRPositionalTracker tracker);

    /// <summary>
    /// Input float changed signal
    /// </summary>
    /// <param name="value">New float value</param>
    /// <param name="tracker">Tracker that emitted the input float action</param>
    [Signal]
    public delegate void InputFloatChangedEventHandler(double value, XRPositionalTracker tracker);

    /// <summary>
    /// Input Vector2 changed signal
    /// </summary>
    /// <param name="value">New Vector2 value</param>
    /// <param name="tracker">Tracker that emitted the input Vector2 action</param>
    [Signal]
    public delegate void InputVector2ChangedEventHandler(Vector2 value, XRPositionalTracker tracker);

    /// <summary>
    /// Pose changed signal
    /// </summary>
    /// <param name="pose">New pose</param>
    /// <param name="tracker">Tracker that emitted the pose</param>
    [Signal]
    public delegate void PoseChangedEventHandler(XRPose pose, XRPositionalTracker tracker);

    /// <summary>
    /// Pose lost tracking signal
    /// </summary>
    /// <param name="pose">Pose</param>
    /// <param name="tracker">Tracker that lost the pose tracking</param>
    [Signal]
    public delegate void PoseLostTrackingEventHandler(XRPose pose, XRPositionalTracker tracker);

    /// <summary>
    /// Gets or sets the type of data being listened for
    /// </summary>
    [Export]
    public DataType Type
    {
        get => _type;
        set => _SetDataType(value);
    }

    /// <summary>
    /// Gets or sets the action name in the OpenXR Action Map
    /// </summary>
    [Export]
    public string Action { get; set; }

    /// <summary>
    /// When we're added to the tree
    /// </summary>
    public override void _EnterTree()
    {
        _SubscribeAll();
        XRServer.TrackerAdded += _OnTrackerAdded;
        XRServer.TrackerRemoved += _OnTrackerRemoved;
    }

    /// <summary>
    /// When we get removed from the tree
    /// </summary>
    public override void _ExitTree()
    {
        XRServer.TrackerAdded -= _OnTrackerAdded;
        XRServer.TrackerRemoved -= _OnTrackerRemoved;
        _UnsubscribeAll();
    }

    /// <summary>
    /// Handles changes to the data type
    /// </summary>
    /// <param name="newType">Type of data to listen for</param>
    private void _SetDataType(DataType newType)
    {
        // Unsubscribe old listeners
        if (IsInsideTree())
            _UnsubscribeAll();

        // Change the type
        _type = newType;

        // Subscribe new listeners
        if (IsInsideTree())
            _SubscribeAll();
    }

    /// <summary>
    /// Unsubscribe listeners from all trackers
    /// </summary>
    private void _UnsubscribeAll()
    {
        foreach (var tracker in XRServer.GetTrackers((int)XRServer.TrackerType.Any))
            _OnTrackerRemoved(tracker.Key.As<StringName>(), (int)XRServer.TrackerType.Any);
    }

    /// <summary>
    /// Subscribe listeners to all trackers
    /// </summary>
    private void _SubscribeAll()
    {
        foreach (var tracker in XRServer.GetTrackers((int)XRServer.TrackerType.Any))
            _OnTrackerAdded(tracker.Key.As<StringName>(), (int)XRServer.TrackerType.Any);
    }

    /// <summary>
    /// Start listening on a tracker
    /// </summary>
    /// <param name="trackerName">Tracker name</param>
    /// <param name="type">Type of tracker</param>
    private void _OnTrackerAdded(StringName trackerName, long type)
    {
        // Get the tracker
        var tracker = XRServer.GetTracker(trackerName);
        if (tracker == null)
            return;

        // Construct the listener
        var listener = new XRActionListener(this, tracker);
        _listeners.Add(trackerName, listener);

        // Bind tracker events
        switch (Type)
        {
            case DataType.Bool:
                tracker.ButtonPressed += listener.OnButtonPressed;
                tracker.ButtonReleased += listener.OnButtonReleased;
                break;

            case DataType.Float:
                tracker.InputFloatChanged += listener.OnInputFloatChanged;
                break;

            case DataType.Vector2:
                tracker.InputVector2Changed += listener.OnInputVector2Changed;
                break;

            case DataType.Pose:
                tracker.PoseChanged += listener.OnPoseChanged;
                tracker.PoseLostTracking += listener.OnPoseLostTracking;
                break;
        }
    }

    /// <summary>
    /// Stop listening to a tracker
    /// </summary>
    /// <param name="trackerName">Tracker name</param>
    /// <param name="type">Tracker type</param>
    private void _OnTrackerRemoved(StringName trackerName, long type)
    {
        // Get the tracker
        if (!_listeners.TryGetValue(trackerName, out var listener))
            return;

        // Remove the tracker
        _listeners.Remove(trackerName);

        // Unsubscribe tracker events
        switch (Type)
        {
            case DataType.Bool:
                listener.Tracker.ButtonPressed -= listener.OnButtonPressed;
                listener.Tracker.ButtonReleased -= listener.OnButtonReleased;
                break;

            case DataType.Float:
                listener.Tracker.InputFloatChanged -= listener.OnInputFloatChanged;
                break;

            case DataType.Vector2:
                listener.Tracker.InputVector2Changed -= listener.OnInputVector2Changed;
                break;

            case DataType.Pose:
                listener.Tracker.PoseChanged -= listener.OnPoseChanged;
                listener.Tracker.PoseLostTracking -= listener.OnPoseLostTracking;
                break;
        }
    }
}