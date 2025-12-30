using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Godot;
using Godot.Collections; //Important to not Confuse with c# System.Collections.Generic
public partial class Lobby : Node
{
    //Static instace that is always Accesable by all scripts
    public static Lobby Instance { get; private set; }

    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void PlayerConnectedEventHandler(int peerId, Dictionary<string, string> playerInfo);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);
    [Signal]
    public delegate void ServerDisconnectedEventHandler();

    /*
    Variables for setting up the server
    */
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
        {"PlayerReady", "True"}
    };


    /*
    Variables that for example only the server keeps track on
    Only Relevant in the Section: Methods for handling any other signals for game logic
    */
    private Dictionary<long, bool> _playersReady = new Dictionary<long, bool>();

    // Character Select data            !!!!!!!!!Neu!!!!!!!!!
    //private Dictionary<long, int> _characterChoices = new Dictionary<long, int>();
    //private Dictionary<long, bool> _characterReady = new Dictionary<long, bool>();//Ende neu

    public int GetChoiseFor(int playerID)
    {
        string characterString = _players[playerID]["CharacterID"];
        return Int32.Parse(characterString);
        //if (_characterChoices.TryGetValue(playerID, out int choise))
        //   return choise;
        //return 0;
    }

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
    // Character Select Signals !!!!!!!!!Neu!!!!!!!!!
    public delegate void OnCharacterChoiseChangedEventHandler(int playerID, int characterIndex);
    [Signal]
    public delegate void CharacterReadyChangedEventHandler(int playerID, bool ready);
    [Signal]
    public delegate void AllCharactersReadyEventHandler(bool allReady);//Ende neu

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
        int newPlayerId = Multiplayer.GetRemoteSenderId();
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
    Every client calls this method when a client on the lobby Sets itself to ready
    */
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayerReady()
    {
        long senderID = Multiplayer.GetRemoteSenderId();

        //Only Server keeps track if all clients are ready
        if (Multiplayer.IsServer())
        {
            //Check if player is already in dictionary
            if (_playersReady.ContainsKey(senderID))
            {
                //Mark player as ready
                _playersReady[senderID] = true;
            }
            else
            {
                //Add new Player to the dictionary
                _playersReady.Add(senderID, true);
            }

            //Check if all players in the lobby are ready
            if (_playersReady.Keys.Count == _players.Keys.Count && !_playersReady.Keys.Except(_players.Keys).Any())
            {
                //Check if all values are tru
                bool allReady = true;
                foreach (bool val in _playersReady.Values)
                {
                    if (val == false)
                    {
                        allReady = false;
                        break;
                    }
                }

                //Emit signal when all are ready
                if (allReady)
                {
                    //Emits the signal to the ui manager to enable the start game button
                    EmitSignal(SignalName.EnableStartGame, null);
                    GD.Print("All Clients are ready");
                }
            }
        }

        EmitSignal(SignalName.SetClientReady, (int)senderID);
    }

    /*
    Every client calls this method when a client on the lobby Sets itself to not ready
    */
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayerNotReady()
    {
        long senderID = Multiplayer.GetRemoteSenderId();

        //Only Server keeps track if all clients are ready
        if (Multiplayer.IsServer())
        {
            //Check if player is already in dictionary
            if (_playersReady.ContainsKey(senderID))
            {
                //Mark player as ready
                _playersReady[senderID] = false;
            }
            else
            {
                //Add new Player to the dictionary
                _playersReady.Add(senderID, false);
            }

            //Hide start button
            EmitSignal(SignalName.DisableStartGame, null);

        }

        //Check if sender is same as receiver
        EmitSignal(SignalName.SetClientNotReady, (int)senderID);
    }

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

    /*
    Server call this method when client request states to be synchronized
    */
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ServerSendSynchronizeStates(int playerID, bool ready)
    {
        // Runs on all peers, including clients
        EmitSignal(SignalName.SendClientState, playerID, ready);
    }

    /*
Client sends this to request a state from the server
Sends own id + id it request the ready state from
*/
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClientRequestReady(int clientPlayerID, int clientRequestID)
    {
        if (Multiplayer.IsServer())
        {
            //Only Server Responses
            EmitSignal(SignalName.ClientRequestState, clientPlayerID, clientRequestID);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void GoToGameScene()
    {
        GD.Print("RPC: Switching to GameScene");
        GetTree().ChangeSceneToFile("res://Scenes/map.tscn");
    }


    /*
        // Character Select Methods !!!!!!!!!Neu!!!!!!!!!
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void CS_SetChoice(int characterIndex)
        {
            string characterString = characterIndex.ToString();
            long senderID = Multiplayer.GetRemoteSenderId();
            _players[senderID]["CharacterID"] = characterString;

            if (!Multiplayer.IsServer()) return;

            long senderID = Multiplayer.GetRemoteSenderId();
            if (senderID == 0)
                senderID = Multiplayer.GetUniqueId();

            _characterChoices[senderID] = characterIndex;

            Rpc(nameof(CS_ReceiveChoice), (int)senderID, characterIndex);

            Rpc(nameof(CS_ReceiveChoice), (int)senderID, characterIndex);
            GD.Print($"[Lobby] CS_SetChoice sender={senderID} index={characterIndex}");
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void CS_ReceiveChoice(int playerID, int characterIndex)
        {
            _characterChoices[playerID] = characterIndex;
            EmitSignal(SignalName.OnCharacterChoiseChanged, playerID, characterIndex);

            GD.Print($"[Lobby] CS_ReceiveChoice player={playerID} index={characterIndex}");
        }
    */

    /// <summary>
    /// CLient calls Method on Server to update
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ClientUpdateCharacter(int characterIndex)
    {
        long senderID = Multiplayer.GetRemoteSenderId();

        if (Multiplayer.IsServer())
        {
            GD.Print($"[Lobby HOST] Broadcast Character Update User: {senderID} Character index: {characterIndex}");
            Rpc(nameof(BroadcastClientUpdateCharacter), senderID, characterIndex);
        }
    }

    /// <summary>
    /// Server calls this method and runs on each client
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void BroadcastClientUpdateCharacter(long senderID, int characterIndex)
    {
        string characterString = characterIndex.ToString();
        _players[senderID]["CharacterID"] = characterString;
        GD.Print($"[Lobby] Client: {Multiplayer.GetUniqueId()} Characterupdate User:{senderID} Character Index: {characterIndex}");

        EmitSignal(SignalName.OnCharacterChoiseChanged, senderID, characterIndex);
    }


    /// <summary>
    /// CLient calls Method on Server to update
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ClientUpdateReady(bool ready)
    {
        long senderID = Multiplayer.GetRemoteSenderId();

        if (Multiplayer.IsServer())
        {
            GD.Print($"[Lobby HOST] Broadcast Ready Update User: {senderID} Ready: {ready}");
            Rpc(nameof(BroadcastClientUpdateReady), senderID, ready);
        }
    }

    /// <summary>
    /// Server calls this method and runs on each client
    /// </summary>

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void BroadcastClientUpdateReady(long senderID, bool ready)
    {
        string readyString = ready.ToString();
        _players[senderID]["PlayerReady"] = readyString;
        GD.Print($"client: {Multiplayer.GetUniqueId()} Readyupdate user:{senderID} Ready: {readyString}");

        EmitSignal(SignalName.CharacterReadyChanged, senderID, ready);

        //Server logic 
        if (Multiplayer.IsServer())
        {
            bool allReady = true;
            foreach (long id in _players.Keys)
            {
                bool result = bool.TryParse(_players[senderID]["PlayerReady"], out bool value) && value;

                if (result == false)
                {
                    allReady = false;
                    break;
                }
            }
            EmitSignal(SignalName.AllCharactersReady, allReady);
        }
    }




    /*[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void CS_SetReady(bool ready)

    {
        long senderID = Multiplayer.GetRemoteSenderId();

        if (!Multiplayer.IsServer()) return;

        _characterReady[senderID] = ready;

        Rpc(nameof(CS_ReceiveReady), (int)senderID, ready);

        bool allReady = true;
        foreach (long id in _players.Keys)
        {
            if (!_characterReady.ContainsKey(id) || !_characterReady[id])
            {
                allReady = false;
                break;
            }
        }

        Rpc(nameof(CS_AllReady), allReady);
    }



    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void CS_ReceiveReady(int playerID, bool ready)
    {
        EmitSignal(SignalName.CharacterReadyChanged, playerID, ready);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void CS_AllReady(bool allReady)
    {
        EmitSignal(SignalName.AllCharactersReady, allReady);
    }
*/



    /// <summary>
    /// CLient calls Method on Server to update
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ClientUpdate()
    {
        long senderID = Multiplayer.GetRemoteSenderId();

        if (Multiplayer.IsServer())
        {
            Rpc(nameof(BroadcastClientUpdate), senderID);
        }

    }

    /// <summary>
    /// Server calls this method and runs on each client
    /// </summary>

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void BroadcastClientUpdate(long senderID)
    {

    }




}//Ende neu