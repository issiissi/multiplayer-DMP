using Godot;

public partial class Character : CharacterBody3D
{
    [Export] public float Speed = 5f;

    // These will be synced by MultiplayerSynchronizer
    [Export] public Vector3 SyncedPosition;
    [Export] public Vector3 SyncedRotation;

    public override void _PhysicsProcess(double delta)
    {
        // Only the authority (owner) controls movement
        if (!Multiplayer.IsServer() && Multiplayer.GetRemoteSenderId() != Multiplayer.GetUniqueId())
            return;

        // Basic WASD movement
        Vector3 inputDir = Vector3.Zero;
        if (Input.IsActionPressed("move_forward")) inputDir += Transform.Basis.Z * -1;
        if (Input.IsActionPressed("move_back")) inputDir += Transform.Basis.Z;
        if (Input.IsActionPressed("move_left")) inputDir += Transform.Basis.X * -1;
        if (Input.IsActionPressed("move_right")) inputDir += Transform.Basis.X;

        Velocity = inputDir.Normalized() * Speed;
        MoveAndSlide();

        // Update synced values
        SyncedPosition = GlobalTransform.Origin;
        SyncedRotation = Rotation;
    }

    public override void _Process(double delta)
    {
        // If not authority, apply synced transforms
        if (!IsMultiplayerAuthority())
        {
            GlobalTransform = new Transform3D(
                Basis.FromEuler(SyncedRotation),
                SyncedPosition
            );
        }
    }
}
