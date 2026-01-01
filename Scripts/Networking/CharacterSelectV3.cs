using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterSelectV3 : Node
{
	// Data
	private List<Texture2D> characters = new List<Texture2D>();
	private int myID;
	private int myCharacterIndex = 0;

	// Eigene UI
	private TextureRect myCharacterPreview;
	private Button leftButton;
	private Button rightButton;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print($"[CS] _Ready instance={GetInstanceId()} isServer={Multiplayer.IsServer()}");

		myID = Multiplayer.GetUniqueId();

		Get_Character_Textures();
		Get_UI_Nodes();

		Refresh_MyPreview();
	}

	private void Get_Character_Textures()
	{
		characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cyberpunk.png"));
		characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_knight.png"));
		characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cat.png"));
	}

	private void Get_UI_Nodes()
	{
		myCharacterPreview = GetNode<TextureRect>("../Character_Preview");
		leftButton = GetNode<Button>("../Links");
		rightButton = GetNode<Button>("../Rechts");

		//Add signals to buttons
		leftButton.Pressed += Left_Button_Down;
		rightButton.Pressed += Right_Button_Down;
	}

	private void Left_Button_Down()
	{

		if (characters.Count == 0) return;

		myCharacterIndex = (myCharacterIndex - 1 + characters.Count) % characters.Count;
		GD.Print($"[CS] Left pressed. ClientID={myID} Character Index={myCharacterIndex}");

		Refresh_MyPreview();

		SendChoiceToServer();
	}

	private void Right_Button_Down()
	{
		if (characters.Count == 0) return;

		myCharacterIndex = (myCharacterIndex + 1) % characters.Count;

		GD.Print($"[CS] Right pressed. ClientID={myID} Character Index={myCharacterIndex}");

		Refresh_MyPreview();

		SendChoiceToServer();
	}

	private void Refresh_MyPreview()
	{
		myCharacterPreview.Texture = characters[myCharacterIndex];
	}

	private void SendChoiceToServer()
	{
		if (Multiplayer.MultiplayerPeer == null)
		{
			GD.PrintErr("[CS] No MultiplayerPeer set, cannot send RPC.");
			return;
		}

		GD.Print($"[CS] Sent choice={myCharacterIndex} to server");

		if (Multiplayer.IsServer())
		{
			//Update for server
			Lobby.Instance.ClientUpdateCharacter(myCharacterIndex);
		}
		else
		{
			//Update for clients
			Lobby.Instance.RpcId(1, nameof(Lobby.ClientUpdateCharacter), myCharacterIndex);
		}

	}

}
