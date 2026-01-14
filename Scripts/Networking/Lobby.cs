using System;
using System.ComponentModel;
using Godot;
using Godot.Collections; //Important to not Confuse with c# System.Collections.Generic


public partial class Lobby : Node
{
    //Static instace that is always Accesable by all scripts
    public static Lobby Instance { get; private set; }


    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void PlayerConnectedEventHandler(long peerId, Dictionary<string, string> playerInfo);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);
    [Signal]
    public delegate void ServerDisconnectedEventHandler();


    //Variables for setting up the server
    [Export]
    public int Port = 7000;
    private string DefaultServerIP = "127.0.0.1"; // IPv4 localhost
    [Export]
    private int MaxConnections = 4;

    [Export]
    public Node2D mapContainer;
    [Export]
    public PackedScene[] WorldScenes;

    [Export]
    PlayerSpawner playerSpawner;

    [Signal] public delegate void PlayAgainProgressEventHandler(int pressed, int total, float secondsLeft);
    [Signal] public delegate void PlayAgainExpiredEventHandler();
    [Signal] public delegate void GameRestartedEventHandler();

    [Export] public float PlayAgainWindowSeconds = 15f;

    private System.Collections.Generic.HashSet<long> playAgainPressed = new();
    private bool playAgainWindowOpen = false;
    private ulong playAgainStartMsec = 0;
    private int playAgainToken = 0;
    [Export] public float playAgainProgressInterval = 0.25f;
    private float playAgainProgressAcc = 0f;

    [Export] public PackedScene LifePointsScene;
    private LifePoints lifePointsUI;


    /* 
    This will contain player info for every player that is currently connected to the server
    with the keys being each player's unique IDs.
    It stores values like this <unique ID, <"Name", "Actual Player name>> "Name" is just the key in the Dictionary
    */
    public Dictionary<long, Dictionary<string, string>> _players = new Dictionary<long, Dictionary<string, string>>();

    /*
    This is the local player info. This should be modified locally
    before the connection is made. It will be passed to every other peer.
    For example, the value of "name" can be set to something the player entered in a UI scene.
    */
    public Dictionary<string, string> _playerInfo = new Dictionary<string, string>()
    {
        {"Name", "PlayerName"},
        {"CharacterID", "0"},
        {"PlayerReady", "false"}
    };


    /*
    Variables that for example only the server keeps track on
    Only Relevant in the Section: Methods for handling any other signals for game logic
    */

    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void ClientChangedCharacterEventHandler(long playerID, int characterIndex);
    [Signal]
    public delegate void ClientChangedReadyEventHandler(long playerID, bool ready);
    [Signal]
    public delegate void AllClientsReadyEventHandler(bool allReady);

    [Signal]
    public delegate void GameLoadedEventHandler();

    // on game started
    public override void _EnterTree()
    {
        Instance = this;
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectOk;
        Multiplayer.ConnectionFailed += OnConnectionFail;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    public override void _Ready()
    {
    }

    /*-------------------------------------------------------------------------
    Methods for handling creating and joining a Lobby
    -------------------------------------------------------------------------*/
    /*
    Creates a Multiplayer.MultiplayerPeer that connects to a server
    */
    public Error JoinGame(string address = "")
    {
        if (string.IsNullOrEmpty(address))
        {
            address = DefaultServerIP;
        }

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(address, Port);

        if (error != Error.Ok)
        {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }


    /*
    Creates a Multiplayer.MultiplayerPeer that is also a server
    Server always has the the Multiplayer.GetUniqueId() == 1
    */
    public Error CreateGame()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(Port, MaxConnections);

        if (error != Error.Ok)
        {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        _players[1] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, 1, _playerInfo);

        return Error.Ok;
    }


    /*
    Disconnects a Multiplayer.MultiplayerPeer from the lobby
    */
    public void RemoveMultiplayerPeer()
    {
        // Close server if this instance is the server
        if (Multiplayer.IsServer())
        {
            if (Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enetPeer)
            {
                enetPeer.Close();
            }
        }

        // Disconnect from server if client
        Multiplayer.MultiplayerPeer = null;

        // Clear all player info
        _players.Clear();

        // Emit a signal so the UI or other nodes know the server disconnected
        EmitSignal(SignalName.ServerDisconnected);
    }


    /*-------------------------------------------------------------------------
    Methods for handling new Connection of clients with the lobby
    -------------------------------------------------------------------------*/

    /* 
    When a peer connects, send them my player info.
    This Method is called when Signal Multiplayer.PeerConnected is emitted.
    Every Client and Server calles this method when a new client joins the lobby
    It transmit the _playerInfo to the newly connected client
    */
    private void OnPlayerConnected(long id)
    {
        RpcId(id, MethodName.RegisterPlayer, _playerInfo);
    }


    /*
    This method is Receives _playerInfo from each client in the lobby when this client connects to the lobby
    */
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer(Dictionary<string, string> newPlayerInfo)
    {
        long newPlayerId = Multiplayer.GetRemoteSenderId();
        _players[newPlayerId] = newPlayerInfo;
        EmitSignal(SignalName.PlayerConnected, newPlayerId, newPlayerInfo);

        //Debug Message
        GD.Print("Client: " + Multiplayer.GetUniqueId() + "\t RegisterPlayer: " + newPlayerId);
        GD.Print($"[Lobby] RegisterPlayer: {newPlayerId} Name={newPlayerInfo["Name"]}");
    }


    /*
    This method is Called by the client who connects to the server when connection was successful
    */
    private void OnConnectOk()
    {
        int peerId = Multiplayer.GetUniqueId();
        _players[peerId] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, peerId, _playerInfo);

        //Debug Message
        GD.Print("Client: " + peerId + "\t OnConnectOk");
        GD.Print($"[Lobby] OnConnectOk: {peerId} Name={_playerInfo["Name"]}");
    }


    /*-------------------------------------------------------------------------
    Methods for handling diconnections of clients from the lobby
    -------------------------------------------------------------------------*/

    /*
    This method is called by each client in the lobby when a client disconnects from the lobby
    The Parameter ID is the client that disconnects from the lobby
    */
    private void OnPlayerDisconnected(long id)
    {
        _players.Remove(id);

        EmitSignal(SignalName.PlayerDisconnected, id);

        playAgainPressed.Remove(id);
        if (_players.Count > 0 && playAgainPressed.Count > _players.Count)
            RestartGameServer();

        //Debug Message
        GD.Print("Client: " + Multiplayer.GetUniqueId() + "\t OnPlayerDisconnected: " + id);
    }


    /*
    This method is called by each client when the server disoconnects
    */
    private void OnServerDisconnected()
    {
        int myID = (Multiplayer.MultiplayerPeer != null) ? Multiplayer.GetUniqueId() : 1;
        // Debug Message
        GD.Print($"Client: {myID}\t OnServerDisconnected");

        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
        EmitSignal(SignalName.ServerDisconnected);
    }

    private void OnConnectionFail()
    {
        int myID = (Multiplayer.MultiplayerPeer != null) ? Multiplayer.GetUniqueId() : 1;
        GD.Print($"Client: {myID}\t OnConnectionFail");

        Multiplayer.MultiplayerPeer = null;
    }


    /*-------------------------------------------------------------------------
    Methods for handling any other signals for game logic
    -------------------------------------------------------------------------*/

    /*
    Server call this method on each client when button to start game is pressed
    */
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void GoToGameScene(int selectedMap)
    {
        GD.Print("RPC: Switching to GameScene");
        GD.Print($"[GoToGameScene] localId={(Multiplayer.MultiplayerPeer != null ? Multiplayer.GetUniqueId() : -1)} peer={(Multiplayer.MultiplayerPeer != null)} isServer={Multiplayer.IsServer()}");


        //Load the map
        Node2D map = WorldScenes[selectedMap].Instantiate<Node2D>(); ;
        mapContainer.AddChild(map);

        EnsureLifePointsUI();
        lifePointsUI.Visible = true;

        //GetTree().ChangeSceneToFile("res://Scenes/map.tscn");
        if (Multiplayer.IsServer())
        {
            playerSpawner.SpawnPlayers();
        }
        // SpawnAllPlayer();

        EmitSignal(SignalName.GameLoaded);
    }

    public void ReturnToLobby()
    {
        GD.Print("[Lobby] ReturnToLobby RPC received");

        if(lifePointsUI != null && GodotObject.IsInstanceValid(lifePointsUI))
            lifePointsUI.Visible = false;

        if (mapContainer != null)
        {
            foreach (Node child in mapContainer.GetChildren())
                child.QueueFree();
        }

        var playerContainer = GetNodeOrNull<Node>("/root/Lobby/PlayerContainer");
        if (playerContainer != null)
        {
            foreach (Node child in playerContainer.GetChildren())
                child.QueueFree();
        }

        foreach (long id in _players.Keys)
        {   
            _players[id]["PlayerReady"] = "false";
        }

        foreach (long id in _players.Keys)
            EmitSignal(SignalName.ClientChangedReady, id, false);

        if (Multiplayer.IsServer())
            EmitSignal(SignalName.AllClientsReady, false);

        var ui = GetNodeOrNull<UIManager>("/root/Lobby/UI Manager");
        ui.ShowLobbyMenuAfterGame();
    }





    /// <summary>
    /// runs on server and calls update of characterselect on every client
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClientUpdateCharacter(int characterIndex)
    {
        // Only the server should process the request and decide to broadcast
        if (!Multiplayer.IsServer()) return;

        long senderID = Multiplayer.GetRemoteSenderId();

        // If the Host calls this function directly (not via RPC), senderID is 0. 
        if (senderID == 0) senderID = Multiplayer.GetUniqueId();

        GD.Print($"[Lobby HOST] Received Character Update Request from: {senderID}. Broadcasting...");

        // Trigger the Broadcast. 
        Rpc(nameof(BroadcastClientUpdateCharacter), senderID, characterIndex);
    }


    /// <summary>
    /// every client resieves update on characterselect of client
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void BroadcastClientUpdateCharacter(long senderID, int characterIndex)
    {
        if (_players.ContainsKey(senderID))
        {
            _players[senderID]["CharacterID"] = characterIndex.ToString();
        }

        GD.Print($"[Lobby] Updating User:{senderID} to Index: {characterIndex} (Local ID: {Multiplayer.GetUniqueId()})");

        EmitSignal(SignalName.ClientChangedCharacter, senderID, characterIndex);
    }


    /// <summary>
    /// runs on server and calls update of readystate on every client
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClientUpdateReady(bool ready)
    {
        // Only the server should process the request and decide to broadcast
        if (!Multiplayer.IsServer()) return;

        long senderID = Multiplayer.GetRemoteSenderId();

        // If the Host calls this function directly (not via RPC), senderID is 0. 
        if (senderID == 0) senderID = Multiplayer.GetUniqueId();

        GD.Print($"[Lobby HOST] Received Rady Update Request from: {senderID}. Broadcasting...");

        // Trigger the Broadcast. 
        Rpc(nameof(BroadcastClientUpdateReady), senderID, ready);

    }


    /// <summary>
    /// every client resieves update on readystate of client
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void BroadcastClientUpdateReady(long senderID, bool ready)
    {
        if (_players.ContainsKey(senderID))
        {
            _players[senderID]["PlayerReady"] = ready.ToString();
        }

        GD.Print($"[Lobby] Updating User:{senderID} to Ready: {ready} (Local ID: {Multiplayer.GetUniqueId()})");

        //Emit Signal that character is ready
        EmitSignal(SignalName.ClientChangedReady, senderID, ready);

        //Server Checks if all Clients are ready
        if (Multiplayer.IsServer())
        {
            bool allClientsReady = true;

            foreach (Dictionary<string, string> playerInfo in _players.Values)
            {
                bool clientReady;
                bool.TryParse(playerInfo["PlayerReady"], out clientReady);
                if (!clientReady)
                {
                    allClientsReady = false;
                    break;
                }
            }

            //Debug Statement
            if (allClientsReady)
            {
                GD.Print($"[Lobby HOST] All clients are ready");
            }

            //Signal to host ui if all clients are ready
            EmitSignal(SignalName.AllClientsReady, allClientsReady);
        }
    }


    /// <summary>
    /// CLient calls Method on Server to update
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClientUpdate()
    {
        if (!Multiplayer.IsServer()) return;

        long senderID = Multiplayer.GetRemoteSenderId();

        // If the Host calls this function directly (not via RPC), senderID is 0. 
        if (senderID == 0) senderID = Multiplayer.GetUniqueId();
        if (Multiplayer.IsServer())
        {
            Rpc(nameof(BroadcastClientUpdate), senderID);
        }
    }


    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void BroadcastSpawnClient(long clientID)
    {
        // Now this runs all clients and server

    }


    /// <summary>
    /// Server calls this method and runs on each client
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void BroadcastClientUpdate(long senderID)
    {
        // Now this runs all clients and server
    }

    // character for fitting playerID
    public int GetChoiseFor(int playerID)
    {
        string characterString = _players[playerID]["CharacterID"];
        return Int32.Parse(characterString);

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RequestPlayAgain()
    {
        if(!Multiplayer.IsServer())
            return;
        
        long senderID = Multiplayer.GetRemoteSenderId();
        if (senderID == 0)
            senderID = Multiplayer.GetUniqueId();
        
        StartOrUpdatePlayAgainWindow(senderID);
    }

    private void StartOrUpdatePlayAgainWindow(long senderID)
    {
        if(!playAgainWindowOpen)
        {
            playAgainWindowOpen = true;
            playAgainPressed.Clear();

            playAgainStartMsec = Time.GetTicksMsec();
            playAgainToken++;
            int token = playAgainToken;

            GetTree().CreateTimer(PlayAgainWindowSeconds).Timeout += () => OnPlayAgainTimeout(token);
        }

        playAgainPressed.Add(senderID);

        playAgainProgressAcc = 0f;
        BroadCastPlayAgainProgressServer();

        if(_players.Count > 0 && playAgainPressed.Count >= _players.Count)
            RestartGameServer();
    }

    private void BroadCastPlayAgainProgressServer()
    {
        float elapsed = (Time.GetTicksMsec() - playAgainStartMsec) / 1000f;
        float left = Mathf.Max(0, PlayAgainWindowSeconds - elapsed);

        Rpc(nameof(RpcPlayAgainProgress), playAgainPressed.Count, _players.Count, left);
    }

    private void OnPlayAgainTimeout(int token)
    {
        if(!Multiplayer.IsServer())
            return;
        if(token != playAgainToken)
            return;
        if(!playAgainWindowOpen)
            return;
        if(_players.Count > 0 && playAgainPressed.Count >= _players.Count)
            return;
        
        playAgainWindowOpen = false;
        playAgainPressed.Clear();
        playAgainProgressAcc = 0f;

        Rpc(nameof(RpcPlayAgainExpired));
    }

    private void RestartGameServer()
    {
        playAgainWindowOpen = false;
        playAgainPressed.Clear();
        playAgainProgressAcc = 0f;

        Rpc(nameof(RpcPlayAgainNow));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcPlayAgainProgress(int pressed, int total, float secondsLeft)
    {
        EmitSignal(SignalName.PlayAgainProgress, pressed, total, secondsLeft);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcPlayAgainExpired()
    {
        EmitSignal(SignalName.PlayAgainExpired);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcPlayAgainNow()
    {

        GD.Print($"[Lobby] RpcPlayAgainNow: server={Multiplayer.IsServer()} GMs={GetTree().GetNodesInGroup("GameManagers").Count}");

        EmitSignal(SignalName.GameRestarted);

        var gms = GetTree().GetNodesInGroup("GameManagers");
        var pls = GetTree().GetNodesInGroup("Players");

        GD.Print($"[Lobby] RpcPlayAgainNow: server={Multiplayer.IsServer()} GMs={gms.Count} Players={pls.Count}");

        foreach (Node gm in gms)
            GD.Print($"[Lobby] GM in group: {gm.GetPath()}");

        // --- 1) GameManager reset (game_Over=false + UI weg) ---
        GetTree().CallGroup("GameManagers", "PlayAgainRoundLocal");

        // --- 2) Player reset (jeder Owner setzt sich auf Spawn) ---
        GetTree().CallGroup("Players", "PlayAgainNewRound");
    }

    public void ClearGameWorldLocal()
    {
        if(mapContainer != null)
        {
            foreach(Node child in mapContainer.GetChildren())
                child.QueueFree();
        }

        if(playerSpawner != null)
        {
            Node parent = playerSpawner.GetNodeOrNull<Node>(playerSpawner.SpawnPath);
            if(parent != null)
            {
                foreach(Node child in parent.GetChildren())
                    child.QueueFree();
            }
        }
    }

    public override void _Process(double delta)
    {
        if(Multiplayer.MultiplayerPeer == null)
            return;
        if(!Multiplayer.IsServer())
            return;
        if(!playAgainWindowOpen)
            return;
        
        playAgainProgressAcc += (float)delta;
        if(playAgainProgressAcc < playAgainProgressInterval)
            return;

        playAgainProgressAcc = 0f;
        BroadCastPlayAgainProgressServer();
    }

    private void EnsureLifePointsUI()
    {
        if(lifePointsUI != null && GodotObject.IsInstanceValid(lifePointsUI))
            return;
        if(LifePointsScene == null)
        {
            GD.PushError("[Lobby] LifePointsScene ist nicht im Inspector gesetzt!");
            return;
        }

        lifePointsUI = LifePointsScene.Instantiate<LifePoints>();

        AddChild(lifePointsUI);
    }

}