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
    public TextureRect bigCharacterPreview;

    [Export]
    public PackedScene playerInfoTemplate;

    public int selectedMapIndex = 0;



    //Dictionary with UI for each player
    private Dictionary<long, Control> playersInfoUI = new Dictionary<long, Control>();

    //Dictionary with Images for Character
    private Dictionary<int, Texture2D> characterImages = new Dictionary<int, Texture2D>();
    private int currentCharacterIndex = 0;

    public override void _Ready()
    {
        //Load images of character
        LoadCharacterImages();
        UpdateBigCharacterPreview(currentCharacterIndex);

        //Display the start menu
        ShowStartMenu();

        //Subscribe to server events
        Lobby.Instance.PlayerConnected += AddPlayerInfoToLobbyMenu;
        Lobby.Instance.PlayerDisconnected += RemovePlayerInfoFromLobbyMenu;
        Lobby.Instance.AllClientsReady += ChangeVisibilityStartButton;
        Lobby.Instance.ClientChangedReady += ClientUpdateReady;
        Lobby.Instance.ClientChangedCharacter += ClientUpdateCharacter;
        Lobby.Instance.GameLoaded += HideAllUI;
    }

    /*************************************************************************
    Code for Hosting Server
    **************************************************************************/
    public void StartHostingServer()
    {
        //Get the name entered in the start menu
        LineEdit nameField = startMenu.GetNode<LineEdit>("Name Input");
        string name = nameField.Text;

        Lobby.Instance._playerInfo["Name"] = name;
        Lobby.Instance._playerInfo["CharacterID"] = currentCharacterIndex.ToString();
        Lobby.Instance._playerInfo["PlayerReady"] = "false";

        //Try starting a server
        Error error = Lobby.Instance.CreateGame();
        if (error != Error.Ok)
        {
            GD.Print("Failed Creating a Server");
            return;
        }

        //Display the Lobby menu in Host view
        ShowLobbyMenuHost();

        //Assign ip address and port to the menu elements
        string serverIp = GetCurrentlyConnectedIPv4(); //Look in Documentation to determen the correct IP of the server in the Array of IPs
        string serverPort = Lobby.Instance.Port.ToString();

        //Add text to line edit
        LineEdit ipField = hostMenu.GetNode<LineEdit>("IP Field");
        LineEdit portField = hostMenu.GetNode<LineEdit>("Port Field");
        ipField.Text = serverIp;
        portField.Text = serverPort;
    }

    /*************************************************************************
    Code for Joining Server
    **************************************************************************/
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
        Lobby.Instance._playerInfo["CharacterID"] = currentCharacterIndex.ToString();
        Lobby.Instance._playerInfo["PlayerReady"] = "false";


        //Try Joining the Lobby
        Error error = Lobby.Instance.JoinGame(ip);
        if (error != Error.Ok)
        {
            GD.Print("Joining Server Failed");
            lobbyMenu.Hide();
        }
        GD.Print("Joined Server");
        ShowLobbyMenuClient();
    }


    /*************************************************************************
    Code for name 
    **************************************************************************/
    private void ClientUpdateName(long senderID, string name)
    {
        Control playerInfoTemplate = playersInfoUI[senderID];
        playerInfoTemplate.GetNode<LineEdit>("Client Name").Text = name;
    }

    /*************************************************************************
    Code for Ready Checkbox
    **************************************************************************/

    /// <summary>
    /// When own client changes to ready or not ready
    /// </summary>
    private void SetLocalClientReady(bool ready)
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


    /// <summary>
    /// Update checkbox UI when other clients update ready over network
    /// </summary>
    private void ClientUpdateReady(long senderID, bool ready)
    {
        Control playerInfoTemplate = playersInfoUI[senderID];
        playerInfoTemplate.GetNode<CheckBox>("Client Ready").SetPressedNoSignal(ready);
    }


    /*************************************************************************
    Code character preview
    **************************************************************************/

    /// <summary>
    /// Loads the images for character
    /// </summary>
    private void LoadCharacterImages()
    {
        characterImages.Add(0, ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cyberpunk.png"));
        characterImages.Add(1, ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_knight.png"));
        characterImages.Add(2, ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cat.png"));
    }


    private void ClientUpdateCharacter(long senderID, int characterIndex)
    {
        Control playerInfoTemplate = playersInfoUI[senderID];
        playerInfoTemplate.GetNode<TextureRect>("Client Character").Texture = characterImages[characterIndex];
    }


    /*************************************************************************
    Code for showing Game start button and starting game
    **************************************************************************/
    private void ChangeVisibilityStartButton(bool allReady)
    {
        if (allReady)
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
        Lobby.Instance.Rpc(Lobby.MethodName.GoToGameScene, selectedMapIndex);
        GD.Print("HOST: StartGame pressed → switching to GameScene");

        startButton.Hide();
    }


    /*************************************************************************
    Code for Changing own Character
    **************************************************************************/
    public void PreviousCharacter()
    {
        currentCharacterIndex = (currentCharacterIndex - 1 + characterImages.Count) % characterImages.Count;
        UpdateBigCharacterPreview(currentCharacterIndex);
        UpdateCharacterOverNetwork(currentCharacterIndex);
    }

    public void NextsCharacter()
    {
        currentCharacterIndex = (currentCharacterIndex + 1) % characterImages.Count;
        UpdateBigCharacterPreview(currentCharacterIndex);
        UpdateCharacterOverNetwork(currentCharacterIndex);
    }

    private void UpdateBigCharacterPreview(int characterIndex)
    {
        bigCharacterPreview.Texture = characterImages[characterIndex];
    }

    private void UpdateCharacterOverNetwork(int characterIndex)
    {
        if (Multiplayer.IsServer())
        {
            //Update for server
            Lobby.Instance.ClientUpdateCharacter(characterIndex);
        }
        else
        {
            //Update for clients
            Lobby.Instance.RpcId(1, nameof(Lobby.ClientUpdateCharacter), characterIndex);
        }
    }

    /*************************************************************************
    Code for Adding and Remvoing client info on lobby screen
    **************************************************************************/
    /// <summary>
    /// Add new Player to UI when client connects to server
    /// </summary>
    private void AddPlayerInfoToLobbyMenu(long senderID, Dictionary<string, string> newPlayerInfo)
    {
        // Create an instance of the template UI element
        Control newPlayerInfoTemplate = playerInfoTemplate.Instantiate<Control>();

        //Make checkbox pressable or not based on the ID (only local client can is pressable)
        if (senderID == Multiplayer.GetUniqueId())
        {
            newPlayerInfoTemplate.GetNode<CheckBox>("Client Ready").Disabled = false;
            //Add signal to press action
            newPlayerInfoTemplate.GetNode<CheckBox>("Client Ready").Toggled += SetLocalClientReady;
        }
        else
        {
            newPlayerInfoTemplate.GetNode<CheckBox>("Client Ready").Disabled = true;
        }

        //Store the value in the dictonary
        playersInfoUI.Add(senderID, newPlayerInfoTemplate);

        // Add to the container (VBoxContainer)
        playerInfoContainer.AddChild(newPlayerInfoTemplate);

        //Update the name
        string name = newPlayerInfo["Name"];
        ClientUpdateName(senderID, name);

        //Update ready state
        bool ready;
        bool.TryParse(newPlayerInfo["PlayerReady"], out ready);
        ClientUpdateReady(senderID, ready);

        //Update character image
        int characterIndex = Int32.Parse(newPlayerInfo["CharacterID"]);
        ClientUpdateCharacter(senderID, characterIndex);

        GD.Print($"[UI] Local Client: {Multiplayer.GetUniqueId()} add new client: {senderID} client state: " + newPlayerInfo.ToString());
    }

    private void RemovePlayerInfoFromLobbyMenu(int playerID)
    {
        //Delete the template from the scene
        playerInfoContainer.RemoveChild(playersInfoUI[playerID]);

        //Remove the player Info data from the dictionary
        playersInfoUI.Remove(playerID);
    }


    public void LeaveLobby()
    {
        foreach (int key in playersInfoUI.Keys)
        {
            RemovePlayerInfoFromLobbyMenu(key);
        }

        playersInfoUI.Clear();


        //When connected to server leave the server
        if (Multiplayer.MultiplayerPeer != null)
        {
            Lobby.Instance.RemoveMultiplayerPeer();
        }


        //Show menus in start layout
        ShowStartMenu();
    }





    /*************************************************************************
    Code for Showing diffrent UI states
    **************************************************************************/
    private void ShowStartMenu()
    {
        //Hide menus not relevant for start
        lobbyMenu.Hide();
        hostMenu.Hide();
        joinMenu.Hide();
        startButton.Hide();

        //Show menu relevant for start
        startMenu.Show();
    }

    public void ShowJoinMenu()
    {
        //Hide menus not relevant for joining a server
        startMenu.Hide();
        lobbyMenu.Hide();
        hostMenu.Hide();
        startButton.Hide();

        //Show menu relevant for joining
        joinMenu.Show();
    }

    public void ShowLobbyMenuHost()
    {
        //hide irelevant menus
        startMenu.Hide();
        joinMenu.Hide();
        startButton.Hide();

        //Show relevant menus
        lobbyMenu.Show();
        hostMenu.Show();
    }

    private void ShowLobbyMenuClient()
    {
        //Hide menus that are no longer relevant
        joinMenu.Hide();
        startMenu.Hide();
        hostMenu.Hide();
        startButton.Hide();

        //Show relevant menu
        lobbyMenu.Show();
    }

    private void HideAllUI()
    {
        joinMenu.Hide();
        startMenu.Hide();
        hostMenu.Hide();
        startButton.Hide();
        lobbyMenu.Hide();
    }


    /*************************************************************************
    Code for Getting active IPv4 addresse of the network
    **************************************************************************/
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

