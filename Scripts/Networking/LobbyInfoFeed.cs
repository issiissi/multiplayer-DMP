using Godot;
using System;
using Godot.Collections;

public partial class LobbyInfoFeed : RichTextLabel
{
	private RichTextLabel feed;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Visible = true;
		Text = "";

		Lobby.Instance.PlayerConnected += OnClientConnect;
		Lobby.Instance.PlayerDisconnected += OnClientDisconnected;
		Lobby.Instance.ClientChangedReady += OnClientReadyChanged;
		Lobby.Instance.ClientChangedCharacter += OnClinetChangedCharacter;


	}
	private void OnClientConnect(long senderID, Dictionary<string, string> newPlayerInfo)
	{
		string name = newPlayerInfo["Name"];
		AddLine($"{name} (ID {senderID}) joined lobby");
	}
	private void OnClientDisconnected(int peerId)
	{
		AddLine($"Client (ID {peerId}) left lobby");
	}
	private void OnClientReadyChanged(long senderID, bool ready)
	{
		string name = Lobby.Instance._players[senderID]["Name"];
		string text = ready ? "Ready" : "not Ready";
		AddLine($"{name} changed to {text}");
	}
	private void OnClinetChangedCharacter(long senderID, int characterIndex)
	{
		string name = Lobby.Instance._players[senderID]["Name"];
		AddLine($"{name} changed character to {characterIndex}");
	}
	private void AddLine(string msg)
	{
		AppendText(msg + "\n");
		ScrollToLine(GetLineCount());
	}
}
