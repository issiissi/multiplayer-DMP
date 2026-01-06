using Godot;
using System;

public partial class DeathZone : Area2D
{
	public override void _Ready()
	{
		GD.Print("[DeathZone] _Ready ✅ Monitoring=" + Monitoring);
		BodyEntered += on_body_entered;
	}

	public void on_body_entered(Node2D body)
	{
		GD.Print($"[DeathZone] body_entered: {body.Name} ({body.GetType().Name})");

		if (body is Player player && player.IsMultiplayerAuthority())
		{
			GD.Print($"[DeathZone] -> resolved Player: {player.Name} | authId={player.GetMultiplayerAuthority()} | localIsAuth={player.IsMultiplayerAuthority()}");
			player.Respawn();
		}
	}
}
