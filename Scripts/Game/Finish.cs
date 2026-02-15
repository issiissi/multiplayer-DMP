using Godot;
using System;

public partial class Finish : Area2D
{
	[Export] public NodePath GameManagerPath;
	[Export] public NodePath ZielPath = "Ziel";
	private GameManager gm;
	private Marker2D ziel;
	public override void _Ready()
	{
		gm = GetNodeOrNull<GameManager>(GameManagerPath);
		if (gm == null)
			GD.PushError($"[Finish] GameManager NICHT gefunden. Check GameManagerPath im Inspector: '{GameManagerPath}'");

		ziel = GetNodeOrNull<Marker2D>(ZielPath);
		if (ziel == null)
			GD.Print($"[Finish] Ziel Marker NICHT gefunden (Pfad '{ZielPath}'). Fallback: Finish.GlobalPosition.X");

		BodyEntered += _on_finish_body_entered;
	}

// Beim Betreten des Finish-Bereichs wird überprüft, ob der Spieler der Server ist und ob er nicht bereits eliminiert wurde
	public void _on_finish_body_entered(Node2D body)
	{
		GD.Print($"[Finish] ENTER: {body.Name} ({body.GetType().Name}) isServer={Multiplayer.IsServer()}");
		if(!Multiplayer.IsServer())
			return;

		var player = body as Player ?? body.GetParentOrNull<Player>();
		if (player == null)
			return;

		if (gm == null)
		{
			GD.PushError("[Finish] gm ist null -> GameManagerPath im Inspector falsch/leer.");
			return;
		}

		float zielX = ziel != null ? ziel.GlobalPosition.X : GlobalPosition.X;
		long winnerID = player.GetMultiplayerAuthority();

		if(gm.IsEliminated(winnerID))
			return;

		gm.ServerEndRace(winnerID, zielX);
	}
}
