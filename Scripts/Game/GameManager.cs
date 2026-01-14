using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node2D
{
	[Export] public NodePath ResultsUIPath;
	private ScoreUi resultsUI;

	[Export] public int MaxFalls = 3;
	private bool game_Over = false;

	private Dictionary<long, int> fallCount = new();
	private HashSet<long> eliminatedPlayers = new();
	private List<long> eliminatedOrder = new();

	private Dictionary<long, ulong> lastFallMsec = new();
	private ulong FallDebounceMsec = 250;

	 private class PlayerResult
    {
        public long PeerId;
        public string Name;
        public float DistX;
		public bool IsEliminated;
		public int EliminatedIndex;
    }
	public override void _Ready()
	{
		AddToGroup("GameManagers");
		resultsUI = GetNode<ScoreUi>(ResultsUIPath);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ClientReportFall(long claimedPeerID)
	{
		if(!Multiplayer.IsServer())
			return;
		
		long sender = Multiplayer.GetRemoteSenderId();
		if(sender != claimedPeerID)
			GD.Print($"[GameManager] ClientReportFall mismatch: sender={sender} claimed={claimedPeerID} (using sender)");
		
		ServerRegisterFall(sender);
	}

	public void ServerRegisterFall(long peerID)
	{
		if(!Multiplayer.IsServer())
			return;
		if(game_Over)
			return;
		if(eliminatedPlayers.Contains(peerID))
			return;

		ulong now = Time.GetTicksMsec();
		if(lastFallMsec.TryGetValue(peerID, out ulong last) && now - last < FallDebounceMsec)
			return;
		lastFallMsec[peerID] = now;

		if(!fallCount.ContainsKey(peerID))
			fallCount[peerID] = 0;
		
		fallCount[peerID]++;

		int remaining = MaxFalls - fallCount[peerID];

		RpcId(peerID, nameof(RpcLivesChangedLocal), remaining);

		if(remaining <= 0)
		{
			eliminatedPlayers.Add(peerID);
			eliminatedOrder.Add(peerID);

			var p = FindPlayerByPeerId(peerID);
			if(p != null)
				p.RpcId(peerID, nameof(Player.RpcClientSetEliminated), true);
			
			EndByLastPlayerStanding();
		}
		else
		{
			var p = FindPlayerByPeerId(peerID);
			if(p != null)
				p.RpcId(peerID, nameof(Player.RpcClientRespawn));
			else
				GD.Print($"[GameManager] Could not find Player node for peerId={peerID} to respawn.");
		}
	}

	public bool IsEliminated(long peerID) => eliminatedPlayers.Contains(peerID);

	private Player FindPlayerByPeerId(long peerID)
	{
		foreach(Node n in GetTree().GetNodesInGroup("Players"))
		{
			if(n is Player p && p.GetMultiplayerAuthority() == peerID)
				return p;
		}
		return null;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcLivesChangedLocal(int remaining)
	{
		GD.Print($"[Lives] remaining={remaining} (local={Multiplayer.GetUniqueId()})");
		GetTree().CallGroup("LifePoints", "OnLivesChanged", remaining);
	}

	private void EndByLastPlayerStanding()
	{
		if(!Multiplayer.IsServer())
			return;
		if(game_Over)
			return;
		
		var allPlayers = GetTree().GetNodesInGroup("Players");
		if(allPlayers.Count < 2)
			return;
		
		Player lastPlayer = null;
		int aliveCount = 0;

		foreach(Node n in allPlayers)
		{
			if(n is not Player p)
				continue;
			long peerID = p.GetMultiplayerAuthority();

			if(IsEliminated(peerID))
				continue;

			aliveCount++;
			lastPlayer = p;
			
			if(aliveCount > 1)
				return;
		}

		if(aliveCount == 1 && lastPlayer != null)
		{
			long winnerID = lastPlayer.GetMultiplayerAuthority();

			float peudoGoalX = lastPlayer.GlobalPosition.X;

			ServerEndRace(winnerID, peudoGoalX, "Last Alive");
			GD.Print($"[GameManager] Auto-finish: last alive winner={winnerID}");
		}
	}

	public void ServerEndRace(long winnerPeerID, float goalx, string winReason = "Finish")
	{
		if (!Multiplayer.IsServer())
			return;
		if(game_Over)
		{
			GD.Print("[GameManager] Finish ignored -> game_Over is still TRUE");
			return;
		}

		if(eliminatedPlayers.Contains(winnerPeerID))
		{
			GD.Print($"[GameManager] Finish ignored -> winner {winnerPeerID} is eliminated");
			return;
		}
		
		game_Over = true;

		var players = new List<PlayerResult>();

		foreach (Node n in GetTree().GetNodesInGroup("Players"))
		{
			if (n is not Player p)
				continue;
			
			long peerID = p.GetMultiplayerAuthority();
			string name = (Lobby.Instance != null && Lobby.Instance._players.ContainsKey(peerID)) ? Lobby.Instance._players[peerID]["Name"] : peerID.ToString();

			float dist = Mathf.Abs(goalx - p.GlobalPosition.X);

			bool isElim = eliminatedPlayers.Contains(peerID);
			int elimIndex = isElim ? eliminatedOrder.IndexOf(peerID) : -1;
			players.Add(new PlayerResult { PeerId = peerID, Name = name, DistX = dist, IsEliminated = isElim, EliminatedIndex = elimIndex});
		}

		var ordered = players
			.OrderBy(e => e.PeerId == winnerPeerID ? 0 : 1)
			.ThenBy(e => e.PeerId == winnerPeerID ? -1 : 0)
			.ThenBy(e => e.IsEliminated ? 1 : 0)
			.ThenBy(e => e.IsEliminated ? -e.EliminatedIndex : e.DistX)
			.ToList();

		string text = "🏁 Ergebnis\n";
		for (int i = 0; i < ordered.Count; i++)
		{
			var e = ordered[i];
			if (e.PeerId == winnerPeerID)
				text += $"{i + 1}. {e.Name} 🥇 ({winReason})\n";
			else if(e.IsEliminated)
				text += $"{i + 1}. {e.Name} ☠️ (Elim)\n";
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

		fallCount.Clear();
		eliminatedPlayers.Clear();
		eliminatedOrder.Clear();
		lastFallMsec.Clear();
		GetTree().CallGroup("LifePoints", "ResetLives", 3);

		resultsUI.HideResults();
	}
}
