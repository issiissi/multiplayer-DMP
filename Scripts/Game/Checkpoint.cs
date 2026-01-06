using Godot;
using System;

public partial class Checkpoint : Area2D
{
	[Export] public NodePath RespawnPointPath = "Respawnpoint";
	private Marker2D respawnpoint;

	public override void _Ready()
	{
		respawnpoint = GetNodeOrNull<Marker2D>(RespawnPointPath);

		BodyEntered += _on_checkpoint_body_entered;
        
	}

	public void _on_checkpoint_body_entered(Node2D body)
	{
		if (body is Player player && player.IsMultiplayerAuthority())
		{
			var pos = respawnpoint != null ? respawnpoint.GlobalPosition : GlobalPosition;
			player.SetRespawnPoint(pos);
		}
	}
}
