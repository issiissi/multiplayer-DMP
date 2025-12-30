using Godot;
using System;
using Godot.Collections;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public partial class UIManager : Node
{
    [Export]
    public CanvasLayer lobbyMenu;
    [Export]
    public CanvasLayer hostMenu;
    [Export]
    public CanvasLayer startMenu;
    [Export]
    public CanvasLayer joinMenu;
    [Export]
    public Button startButton;
    [Export]
    public VBoxContainer playerInfoContainer;
    [Export]
    public PackedScene playerInfoTemplate;
    private Dictionary<int, Control> playerInfoData = new Dictionary<int, Control>();


    public override void _Ready()
    {
        //Hide all menus except the start menu
        lobbyMenu.Hide();
        hostMenu.Hide();
        joinMenu.Hide();
        startButton.Hide();
        startMenu.Show();

        //Subscribe to server events
        Lobby.Instance.PlayerConnected += AddPlayerInfoToLobbyMenu;
        Lobby.Instance.PlayerDisconnected += RemovePlayerInfoFromLobbyMenu;

        //Lobby.Instance.EnableStartGame += ShowStartButton;
        //Lobby.Instance.DisableStartGame += HideStartButton;

        Lobby.Instance.AllCharactersReady += ChangeVisibilityStartButton;

        Lobby.Instance.GameStarted += GameStarted;

        Lobby.Instance.CharacterReadyChanged += CharacterChangedReady;
    }


    public void StartHostingServer()
    {
        //Get the name entered in the start menu
        LineEdit nameField = startMenu.GetNode<LineEdit>("Name Input");
        string name = nameField.Text;

        Lobby.Instance._playerInfo["Name"] = name;
        Lobby.Instance._playerInfo["CharacterID"] = "0";
        Lobby.Instance._playerInfo["PlayerReady"] = "false";

        //Try starting a server
        Error error = Lobby.Instance.CreateGame();
        if (error != Error.Ok)
        {
            GD.Print("Failed Creating a Server");
            return;
        }

        //When user starts hosting hide irelevant menus
        startMenu.Hide();
        joinMenu.Hide();
        startButton.Hide();
        //Show relevant menus
        lobbyMenu.Show();
        hostMenu.Show();

        //Assign ip address and port to the menu elements
        string serverIp = GetCurrentlyConnectedIPv4(); //Look in Documentation to determen the correct IP of the server in the Array of IPs
        string serverPort = Lobby.Instance.Port.ToString();

        //Add text to line edit
        LineEdit ipField = hostMenu.GetNode<LineEdit>("IP Field");
        LineEdit portField = hostMenu.GetNode<LineEdit>("Port Field");
        ipField.Text = serverIp;
        portField.Text = serverPort;
    }


    public void StartJoiningServer()
    {
        //Hide menus not relevant for joining a server
        startMenu.Hide();
        lobbyMenu.Hide();
        hostMenu.Hide();
        startButton.Hide();

        //Show menu relevant for joining
        joinMenu.Show();
    }


    public void JoinServer()
    {
        //Get the IP, port and name from the input fields
        LineEdit ipField = joinMenu.GetNode<LineEdit>("IP Input");
        LineEdit portField = joinMenu.GetNode<LineEdit>("Port Input");
        LineEdit nameField = startMenu.GetNode<LineEdit>("Name Input");

        string ip = ipField.Text;
        int port = -1;
        Int32.TryParse(portField.Text, out port);
        string name = nameField.Text;


        //Assign values to the lobby instance
        if (port != -1)
        {
            Lobby.Instance.Port = port;
        }

        Lobby.Instance._playerInfo["Name"] = name;
        Lobby.Instance._playerInfo["CharacterID"] = "0";
        Lobby.Instance._playerInfo["PlayerReady"] = "false";


        //Try Joining the Lobby
        Error error = Lobby.Instance.JoinGame(ip);
        if (error != Error.Ok)
        {
            GD.Print("Joining Server Failed");
            GD.Print("Joined Server");
            lobbyMenu.Hide();
        }

        //Hide menus that are no longer relevant
        joinMenu.Hide();
        startMenu.Hide();
        hostMenu.Hide();
        startButton.Hide();

        //Show relevant menu
        lobbyMenu.Show();
    }


    /*************************************************************************
    Code for Ready Checkbox
    **************************************************************************/
    public void CheckBoxToggled(bool ready)
    {
        if (Multiplayer.IsServer())
        {
            //Update for server
            Lobby.Instance.ClientUpdateReady(ready);
        }
        else
        {
            //Update for clients
            Lobby.Instance.RpcId(1, nameof(Lobby.ClientUpdateReady), ready);
        }
    }


    public void CharacterChangedReady(long senderID, bool ready)
    {
        if (ready)
        {
            SetClientToPressed((int)senderID);
        }
        else
        {
            SetClientNotPressed((int)senderID);
        }
    }

    /*************************************************************************
    Code for showing start button
    **************************************************************************/
    public void ChangeVisibilityStartButton(bool visible)
    {
        if (visible)
        {
            startButton.Show();
        }
        else
        {
            startButton.Hide();
        }
    }


    public void StartGame()
    {
        if (Multiplayer.IsServer())
        {
            Lobby.Instance.Rpc(Lobby.MethodName.GoToGameScene);
            GD.Print("HOST: StartGame pressed → switching to GameScene");
        }
        else
        {
            GD.Print("CLIENT pressed StartGame → ignored");
        }
        startButton.Hide();
    }


    public void AddPlayerInfoToLobbyMenu(int playerID, Dictionary<string, string> newPlayerInfo)
    {
        // Create an instance of the template UI element
        Control newPlayerInfoTemplate = playerInfoTemplate.Instantiate<Control>();

        //Load name of the new client
        string name = Lobby.Instance._players[playerID]["Name"];
        newPlayerInfoTemplate.GetNode<LineEdit>("Client Name").Text = name;

        //Load ready state of client
        bool ready;
        bool.TryParse(Lobby.Instance._players[playerID]["PlayerReady"], out ready);
        newPlayerInfoTemplate.GetNode<CheckBox>("Client Ready").SetPressedNoSignal(ready);

        //Make checkbox pressable or not based on the ID
        if (playerID == Multiplayer.GetUniqueId())
        {
            newPlayerInfoTemplate.GetNode<CheckBox>("Client Ready").Disabled = false;
            newPlayerInfoTemplate.GetNode<CheckBox>("Client Ready").Toggled += CheckBoxToggled;
        }
        else
        {
            newPlayerInfoTemplate.GetNode<CheckBox>("Client Ready").Disabled = true;
        }

        //Store the value in the dictonary
        playerInfoData.Add(playerID, newPlayerInfoTemplate);

        // Add to the container (VBoxContainer)
        playerInfoContainer.AddChild(newPlayerInfoTemplate);
    }


    public void RemovePlayerInfoFromLobbyMenu(int playerID)
    {
        //Delete the template from the scene
        playerInfoContainer.RemoveChild(playerInfoData[playerID]);

        //Remove the player Info data from the dictionary
        playerInfoData.Remove(playerID);
    }


    public void SetClientToPressed(int playerID)
    {
        Control playerInfoTemplate = playerInfoData[playerID];
        playerInfoTemplate.GetNode<CheckBox>("Client Ready").SetPressedNoSignal(true);
    }


    public void SetClientNotPressed(int playerID)
    {
        Control playerInfoTemplate = playerInfoData[playerID];
        playerInfoTemplate.GetNode<CheckBox>("Client Ready").SetPressedNoSignal(false);
    }


    //Called by a signal from the server
    public void GameStarted()
    {
        //Loop over each client and set it to not ready
        foreach (int key in playerInfoData.Keys)
        {
            SetClientNotPressed(key);
        }
    }


    public void ResetToStart()
    {
        foreach (int key in playerInfoData.Keys)
        {
            RemovePlayerInfoFromLobbyMenu(key);
        }

        playerInfoData.Clear();


        //When connected to server leave the server
        if (Multiplayer.MultiplayerPeer != null)
        {
            Lobby.Instance.RemoveMultiplayerPeer();
        }


        //Show menus in start layout
        lobbyMenu.Hide();
        hostMenu.Hide();
        joinMenu.Hide();
        startButton.Hide();
        startMenu.Show();
    }


    static string GetCurrentlyConnectedIPv4()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Only Ethernet or Wi-Fi interfaces that are up
            if (ni.OperationalStatus == OperationalStatus.Up &&
                (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                 ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
            {
                var ipProps = ni.GetIPProperties();

                // Check if it has a valid default gateway
                if (ipProps.GatewayAddresses.Count > 0)
                {
                    foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
        }
        return null;
    }
}

