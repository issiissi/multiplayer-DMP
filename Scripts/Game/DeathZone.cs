using Godot;
using System;
using System.ComponentModel;

public partial class DeathZone : Area2D
{
	[Export] public NodePath GameManagerPath;

	private GameManager gameManager;
	public override void _Ready()
	{
		gameManager = GetNode<GameManager>(GameManagerPath);
		if(gameManager == null)
		{
			GD.PrintErr("[DeathZone] GameManager node not found!");
		}

		BodyEntered += on_body_entered;
	}

	public void on_body_entered(Node2D body)
	{
		var player = body as Player ?? body.GetParentOrNull<Player>();
		if(player == null)
			return;
		if(!player.IsMultiplayerAuthority())
			return;

		long peerID = player.GetMultiplayerAuthority();
		GD.Print($"[DeathZone] Fall reported by owner. peerID={peerID} server={Multiplayer.IsServer()}");

		if (gameManager == null)
			return;
		
		if(Multiplayer.IsServer())
			gameManager.ServerRegisterFall(peerID);
		else
			gameManager.RpcId(1, nameof(GameManager.ClientReportFall), peerID);
	}
}
