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

	// Other Players
	private Label[] otherPlayerNames;
	private TextureRect[] otherPlayerCharacterPreviews;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print($"[CS] _Ready instance={GetInstanceId()} isServer={Multiplayer.IsServer()}");

		myID = Multiplayer.GetUniqueId();

		Get_Character_Textures();
		Get_UI_Nodes();

		Refresh_MyPreview();

		Lobby.Instance.RpcId(1, Lobby.MethodName.ClientUpdateCharacter, myCharacterIndex);

		//Refresh_OtherPreviews();
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

		otherPlayerNames = new Label[]
		{
			GetNode<Label>("../Other_Players/Other_Player1/Label"),
			GetNode<Label>("../Other_Players/Other_Player2/Label"),
			GetNode<Label>("../Other_Players/Other_Player3/Label")
		};

		otherPlayerCharacterPreviews = new TextureRect[]
		{
			GetNode<TextureRect>("../Other_Players/Other_Player1/TextureRect"),
			GetNode<TextureRect>("../Other_Players/Other_Player2/TextureRect"),
			GetNode<TextureRect>("../Other_Players/Other_Player3/TextureRect")
		};

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

	private void Refresh_OtherPreviews()
	{
		for (int i = 0; i < 3; i++)
		{
			otherPlayerNames[i].Text = "";
			otherPlayerCharacterPreviews[i].Texture = null;
		}

		int slot = 0;

		foreach (var entry in Lobby.Instance._players)
		{
			int id = (int)entry.Key;
			if (id == myID) continue;
			if (slot >= 3) break;

			string name = entry.Value["Name"];
			int index = Lobby.Instance.GetChoiseFor(id);
			index = Mathf.Clamp(index, 0, characters.Count - 1);

			otherPlayerNames[slot].Text = name;
			otherPlayerCharacterPreviews[slot].Texture = characters[index];

			slot++;
		}
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
