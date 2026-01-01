using System;
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
    [Export]
    private string DefaultServerIP = "127.0.0.1"; // IPv4 localhost
    [Export]
    private int MaxConnections = 4;


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
    private Dictionary<long, bool> _playersReady = new Dictionary<long, bool>();


    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void EnableStartGameEventHandler();
    [Signal]
    public delegate void DisableStartGameEventHandler();
    [Signal]
    public delegate void GameStartedEventHandler();
    [Signal]
    public delegate void SetClientReadyEventHandler(int playerID);
    [Signal]
    public delegate void SetClientNotReadyEventHandler(int playerID);
    [Signal]
    public delegate void SendClientStateEventHandler(int playerID, bool ready);
    [Signal]
    public delegate void ClientRequestStateEventHandler(int playerID, int targetPlayerID);


    [Signal]
    public delegate void OnCharacterChoiseChangedEventHandler(long playerID, int characterIndex);

    [Signal]
    public delegate void CharacterReadyChangedEventHandler(long playerID, bool ready);
    [Signal]
    public delegate void AllClientsReadyEventHandler(bool allReady);

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

        if (Multiplayer.IsServer())
        {
            _playersReady.Remove(id);

        }
        EmitSignal(SignalName.PlayerDisconnected, id);

        //Leave also when server disconnects
        //if(id == 1)
        //{
        //    RemoveMultiplayerPeer();
        //}
        //_characterChoices.Remove(id);
        // _characterReady.Remove(id);
        //Debug Message
        GD.Print("Client: " + Multiplayer.GetUniqueId() + "\t OnPlayerDisconnected: " + id);
    }


    /*
    This method is called by each client when the server disoconnects
    */
    private void OnServerDisconnected()
    {
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
        EmitSignal(SignalName.ServerDisconnected);

        //Debug Message
        GD.Print("Client: " + Multiplayer.GetUniqueId() + "\t OnServerDisconnected");
    }

    private void OnConnectionFail()
    {
        Multiplayer.MultiplayerPeer = null;

        GD.Print("Client: " + Multiplayer.GetUniqueId() + "\t OnConnectionFail");
    }


    /*-------------------------------------------------------------------------
    Methods for handling any other signals for game logic
    -------------------------------------------------------------------------*/

    /*
    Server call this method when button to start game is pressed
    */
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ServerStartGame()
    {
        // Only server updates ready states
        if (Multiplayer.IsServer())
        {
            foreach (long key in _playersReady.Keys)
                _playersReady[key] = false;

            EmitSignal(SignalName.DisableStartGame); // server-only signal
        }

        // Runs on all peers, including clients
        EmitSignal(SignalName.GameStarted);
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void GoToGameScene()
    {
        GD.Print("RPC: Switching to GameScene");
        GetTree().ChangeSceneToFile("res://Scenes/map.tscn");
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

        EmitSignal(SignalName.OnCharacterChoiseChanged, senderID, characterIndex);
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
        EmitSignal(SignalName.CharacterReadyChanged, senderID, ready);

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
}