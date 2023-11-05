using Godot;

namespace GodotOpenXRDemo;

public partial class Player : CharacterBody3D
{
    private float _gravity;
    private XROrigin3D _originNode;
    private XRCamera3D _cameraNode;
    private Node3D _neckPositionNode;
    private objects.BlackOut _blackout;
    private Vector2 _movementInput;

    // Settings to control the character
    [Export] public float RotationSpeed { get; set; } = 1.0f;
    [Export] public float MovementSpeed { get; set; } = 5.0f;
    [Export] public float MovementAcceleration { get; set; } = 5.0f;

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity", 9.8f);
        _originNode = GetNode<XROrigin3D>("XROrigin3D");
        _cameraNode = GetNode<XRCamera3D>("XROrigin3D/XRCamera3D");
        _neckPositionNode = GetNode<Node3D>("XROrigin3D/XRCamera3D/Neck");
        _blackout = GetNode<objects.BlackOut>("XROrigin3D/XRCamera3D/BlackOut");
    }

    /// <summary>
    /// Handle our player movement
    /// </summary>
    /// <param name="delta"></param>
    public override void _PhysicsProcess(double delta)
    {
        var isColliding = ProcessOnPhysicalMovement((float)delta);
        ProcessMovementOnInput(isColliding, (float)delta);
    }

    public void Recenter()
    {
        // Calculate where our camera should be, we start with our global transform
        var newCameraTransform = GlobalTransform;

        // Set to the height of our neck joint
        newCameraTransform.Origin.Y = _neckPositionNode.GlobalPosition.Y;

        // Apply transform our our next position to get our desired camera transform
        newCameraTransform *= _neckPositionNode.Transform.Inverse();

        // Remove tilt from camera transform
        var cameraTransform = _cameraNode.Transform;
        var forwardDir = cameraTransform.Basis.Z;
        forwardDir.Y = 0;
        cameraTransform = cameraTransform.LookingAt(cameraTransform.Origin + forwardDir.Normalized(), Vector3.Up, true);

        // Update our XR location
        _originNode.GlobalTransform = newCameraTransform * cameraTransform.Inverse();
    }

    public void OnMovementChanged(Vector2 value, XRPositionalTracker tracker)
    {
        _movementInput = value;
    }

    /// <summary>
    /// ProcessOnPhysicalMovement handles the physical movement of the player
    /// adjusting our character body position to "catch up to" the player.
    /// If the character body encounters an obstruction our view will black out
    /// and we will stop further character movement until the player physically
    /// moves back.
    /// </summary>
    /// <param name="delta">Time delta</param>
    /// <returns></returns>
    public bool ProcessOnPhysicalMovement(float delta)
    {
        // Remember our current velocity, we'll apply that later
        var currentVelocity = Velocity;

        // Start by rotating the player to face the same way our real player is
        var cameraBasis = _originNode.Transform.Basis * _cameraNode.Transform.Basis;
        var forward = new Vector2(cameraBasis.Z.X, cameraBasis.Z.Z);
        var angle = forward.AngleTo(new Vector2(0.0f, 1.0f));

        // Rotate our character body
        Transform = new Transform3D(
            Transform.Basis.Rotated(Vector3.Up, angle),
            Transform.Origin);

        // Reverse this rotation our origin node
        _originNode.Transform = Transform3D.Identity.Rotated(Vector3.Up, -angle) * _originNode.Transform;

        // Now apply movement, first move our player body to the right location
        var orgPlayerBody = GlobalTransform.Origin;
        var playerBodyLocation = _originNode.Transform * _cameraNode.Transform * _neckPositionNode.Transform.Origin;
        playerBodyLocation.Y = 0.0f;
        playerBodyLocation = GlobalTransform * playerBodyLocation;

        Velocity = (playerBodyLocation - orgPlayerBody) / delta;
        MoveAndSlide();

        // Now move our XROrigin back
        var deltaMovement = GlobalTransform.Origin - orgPlayerBody;
        _originNode.GlobalTransform = new Transform3D(
            _originNode.GlobalTransform.Basis,
            _originNode.GlobalTransform.Origin - deltaMovement);

        // Negate any height change in local space due to player hitting ramps etc.
        _originNode.Transform = new Transform3D(
            _originNode.Transform.Basis,
            _originNode.Transform.Origin with { Y = 0.0f });

        // Return our value
        Velocity = currentVelocity;

        // Check if we managed to move where we wanted to
        var locationOffset = (playerBodyLocation - GlobalTransform.Origin).Length();
        if (locationOffset > 0.1)
        {
            // We couldn't go where we wanted to, black out our screen
            _blackout.Fade = Mathf.Clamp((locationOffset - 0.1F) / 0.1F, 0.0F, 1.0F);
            return true;
        }

        _blackout.Fade = 0.0F;
        return false;
    }

    /// <summary>
    /// ProcessMovementOnInput handles movement through controller input.
    /// # We first handle rotating the player and then apply movement.
    /// # We also apply the effects of gravity at this point.
    /// </summary>
    /// <param name="isColliding"></param>
    /// <param name="delta"></param>
    void ProcessMovementOnInput(bool isColliding, float delta)
    {
        if (!isColliding)
        {
            // First handle rotation, to keep this example simple we are implementing
            // "smooth" rotation here. This can lead to motion sickness.
            // Adding a comfort option with "stepped" rotation is good practice but
            // falls outside of the scope of this demonstration.
            Rotation = Rotation with { Y = Rotation.Y - _movementInput.X * delta * RotationSpeed };

            // Now handle forward/backwards movement.
            // Strafing can be added by using the movement_input.x input
            // and using a different input for rotational control.
            // Strafing is more prone to motion sickness.
            var direction = GlobalTransform.Basis * new Vector3(0.0f, 0.0f, -_movementInput.Y) * MovementSpeed;
            Velocity = new Vector3(
                Mathf.MoveToward(Velocity.X, direction.X, delta * MovementAcceleration),
                Velocity.Y,
                Mathf.MoveToward(Velocity.Z, direction.Z, delta * MovementAcceleration));
        }

        // Always handle gravity
        Velocity = Velocity with { Y = Velocity.Y - _gravity * delta };

        MoveAndSlide();
    }
}