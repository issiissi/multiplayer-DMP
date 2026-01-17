using Godot;
using System;

public partial class Leiter : Area2D
{
	public override void _Ready()
	{
		GD.Print($"[Leiter] Ready ✅ Monitoring={Monitoring}");
		BodyEntered += _on_leiter_body_entered;
		BodyExited += _on_leiter_body_exited;

	}

	public void _on_leiter_body_entered(Node2D body)
	{
		GD.Print($"[Leiter] ENTER: {body.Name} ({body.GetType().Name})");
		if (body is Player player && player.IsMultiplayerAuthority())
			player.SetOnLadder(true);
	}

	public void _on_leiter_body_exited(Node2D body)
	{
		GD.Print($"[Leiter] EXIT: {body.Name} ({body.GetType().Name})");
		if (body is Player player && player.IsMultiplayerAuthority())
			player.SetOnLadder(false);
	}
}
