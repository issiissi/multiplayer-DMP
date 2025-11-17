using Godot;
using System;

public partial class MultiplayerManager : Node
{
	[Export] PackedScene player;
	[Export] Control menu;

	private const int Port = 7777;
	private const string HostAddress = "localhost";
	ENetMultiplayerPeer peer;

	public override void _Ready()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
	}

	public void StartHost()
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateServer(Port);

		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Host started on port " + Port);

		// Hide menu
		menu.Hide();

		// Host spawns its own player
		SpawnPlayer(Multiplayer.GetUniqueId());
	}

	public void StartClient()
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateClient(HostAddress, Port);

		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Client connecting to " + HostAddress);

		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnDisconnectedFromServer;

		// Hide menu
		menu.Hide();
	}

	private void OnPeerConnected(long id)
	{
		GD.Print($"Peer {id} connected");

		// Only host runs this
		if (Multiplayer.IsServer())
			SpawnPlayer((int)id);
	}

	private void OnPeerDisconnected(long id)
	{
		GD.Print($"Peer {id} disconnected");

		// You can remove their player here if needed
	}

	private void OnConnectionFailed()
	{
		GD.Print("Connection to host failed!");
	}

	private void OnDisconnectedFromServer()
	{
		GD.Print("Lost connection to host");
	}

	private void SpawnPlayer(long id)
	{
		var p = player.Instantiate<Node3D>();

		p.SetMultiplayerAuthority((int)id);

		// IMPORTANT: add to the CURRENT SCENE, not a container
		GetTree().CurrentScene.AddChild(p);

		GD.Print($"Spawned player for peer {id}");
	}

}
