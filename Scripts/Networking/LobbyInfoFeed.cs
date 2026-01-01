using Godot;
using System;
using System.Collections.Generic;
using GDict = Godot.Collections.Dictionary<string, string>;

public partial class LobbyInfoFeed : RichTextLabel
{
	private RichTextLabel feed;

	private Dictionary<int, bool> ready = new Dictionary<int, bool>();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("[Feed] _Ready läuft ✅");
    	Visible = true;
		Text = "Feed online ✅\n";

		Lobby.Instance.PlayerConnected += OnPlayerConnected;
		Lobby.Instance.PlayerDisconnected += OnPlayerDisconnected;
		Lobby.Instance.SetClientReady += OnClientReady;
		Lobby.Instance.SetClientNotReady += OnClientNotReady;

		foreach (var entry in Lobby.Instance._players)
			OnPlayerConnected((int)entry.Key, entry.Value);
	}

	private void OnPlayerConnected(long peerId, GDict info)
    {
        string name = info.ContainsKey("Name") ? info["Name"] : $"Player {peerId}";
        AddLine($"➡️ {name} (ID {peerId}) ist gejoined.");
    }

    private void OnPlayerDisconnected(int peerId) => AddLine($"⬅️ Player (ID {peerId}) hat die Lobby verlassen.");
    private void OnClientReady(int playerId) => AddLine($"🟢 Player {playerId} ist READY.");
    private void OnClientNotReady(int playerId) => AddLine($"🔴 Player {playerId} ist NOT READY.");

    private void AddLine(string msg)
    {
        AppendText(msg + "\n");
		ScrollToLine(GetLineCount());
    }
}
