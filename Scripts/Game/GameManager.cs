using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node2D
{
	[Export] public NodePath ResultsUIPath;
	private ScoreUi resultsUI;

	private bool game_Over = false;

	 private class PlayerResult
    {
        public long PeerId;
        public string Name;
        public float DistX;
    }
	public override void _Ready()
	{
		AddToGroup("GameManagers");
		resultsUI = GetNode<ScoreUi>(ResultsUIPath);
	}

	public void ServerEndRace(long winnerPeerID, float goalx)
	{
		if (!Multiplayer.IsServer())
			return;
		if(game_Over)
		{
			GD.Print("[GameManager] Finish ignored -> game_Over is still TRUE");
			return;
		}
		
		game_Over = true;

		var players = new List<PlayerResult>();

		foreach (Node n in GetTree().GetNodesInGroup("Players"))
		{
			if (n is not Player p)
				continue;
			
			long pid = p.GetMultiplayerAuthority();
			string name = Lobby.Instance._players[pid]["Name"];

			float dist = Mathf.Abs(goalx - p.GlobalPosition.X);
			players.Add(new PlayerResult { PeerId = pid, Name = name, DistX = dist});
		}

		var ordered = players
			.OrderBy(e => e.PeerId == winnerPeerID ? 0 : 1)
			.ThenBy(e => e.PeerId == winnerPeerID ? -1 : e.DistX)
			.ToList();

		string text = "🏁 Ergebnis\n";
		for (int i = 0; i < ordered.Count; i++)
		{
			var e = ordered[i];
			if (e.PeerId == winnerPeerID)
				text += $"{i + 1}. {e.Name} 🥇 (Finish)\n";
			else
				text += $"{i + 1}. {e.Name} (DistX: {e.DistX:0})\n";
		}

		Rpc(nameof(RpcApplyGameOverAndShowResults), text);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcApplyGameOverAndShowResults(string resultsText)
	{
		GetTree().CallGroup("Players", "SetGameOver", true);

		resultsUI.show_Results(resultsText);
	}

	public void PlayAgainRoundLocal()
	{
		GD.Print($"[GameManager] PlayAgainRoundLocal CALLED (server={Multiplayer.IsServer()}) -> game_Over=false");

		GD.Print($"[GameManager] PlayAgainRoundLocal called (server={Multiplayer.IsServer()}) -> resetting game_Over");
		game_Over = false;

		resultsUI.HideResults();
	}
}
