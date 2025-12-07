using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterSelect : Control
{
	// Called when the node enters the scene tree for the first time.

	private List<Texture2D> characters = new List<Texture2D>();
	private int p1Index = 0;
	private int p2Index = 0;

	private int p3Index = 0;
	private int p4Index = 0;
	private bool p1Ready = false;
	private bool p2Ready = false;
	private bool p3Ready = false;
	private bool p4Ready = false;
	private TextureRect p1Preview;
	private TextureRect p2Preview;
	private TextureRect p3Preview;
	private TextureRect p4Preview;

	private Label p1Name;
	private Label p2Name;
	private Label p3Name;
	private Label p4Name;
	private Button startButton;
	public override void _Ready()
    {
        characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cyberpunk.png"));
		characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_knight.png"));
		characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cat.png"));

		p1Preview = GetNode<TextureRect>("MarginContainer/HBoxContainer/Player1/CharacterPreview");
		p2Preview = GetNode<TextureRect>("MarginContainer/HBoxContainer/Player2/CharacterPreview");
		p3Preview = GetNode<TextureRect>("MarginContainer/HBoxContainer/Player3/CharacterPreview");
		p4Preview = GetNode<TextureRect>("MarginContainer/HBoxContainer/Player4/CharacterPreview");
		startButton = GetNode<Button>("StartButton");

		p1Name = GetNode<Label>("MarginContainer/HBoxContainer/Player1/Name");
		p2Name = GetNode<Label>("MarginContainer/HBoxContainer/Player2/Name");
		p3Name = GetNode<Label>("MarginContainer/HBoxContainer/Player3/Name");
		p4Name = GetNode<Label>("MarginContainer/HBoxContainer/Player4/Name");

		Load_Names();
		Update_Previews();
    }
	private void Update_Previews()
    {
        p1Preview.Texture = characters[p1Index];
		p2Preview.Texture = characters[p2Index];
		p3Preview.Texture = characters[p3Index];
		p4Preview.Texture = characters[p4Index];
    }
	private void Load_Names()
    {
        var players = Lobby.Instance._players;
		foreach (var entry in players)
        {
            long id = entry.Key;
			string name = entry.Value["Name"];

			if (id == 1) p1Name.Text = name;
			if (id == 2) p2Name.Text = name;
			if (id == 3) p3Name.Text = name;
			if (id == 4) p4Name.Text = name;
        }
    }
	private void _on_links_player_1_button_down()
    {
        if (p1Ready) 
			return;
		p1Index = (p1Index - 1 + characters.Count) % characters.Count;
		Update_Previews();
    }
	private void _on_rechts_player_1_button_down()
    {
       if (p1Ready) 
			return;
		p1Index = (p1Index + 1) % characters.Count;
		Update_Previews();
    }
	private void _on_ready_player_1_button_down()
    {
        p1Ready = !p1Ready;
		CheckBothReady();
    }
	private void _on_links_player_2_button_down()
    {
        if (p2Ready) 
			return;
		p2Index = (p2Index - 1 + characters.Count) % characters.Count;
		Update_Previews();
    }
	private void _on_rechts_player_2_button_down()
    {
       if (p2Ready) 
			return;
		p2Index = (p2Index + 1) % characters.Count;
		Update_Previews();
    }
	private void _on_ready_player_2_button_down()
    {
        p2Ready = !p2Ready;
		CheckBothReady();
    }
	private void _on_links_player_3_button_down()
    {
        if (p3Ready) 
			return;
		p3Index = (p3Index - 1 + characters.Count) % characters.Count;
		Update_Previews();
    }
	private void _on_rechts_player_3_button_down()
    {
       if (p3Ready) 
			return;
		p3Index = (p3Index + 1) % characters.Count;
		Update_Previews();
    }
	private void _on_ready_player_3_button_down()
    {
        p3Ready = !p3Ready;
		CheckBothReady();
    }
	private void _on_links_player_4_button_down()
    {
        if (p4Ready) 
			return;
		p4Index = (p4Index - 1 + characters.Count) % characters.Count;
		Update_Previews();
    }
	private void _on_rechts_player_4_button_down()
    {
       if (p4Ready) 
			return;
		p4Index = (p4Index + 1) % characters.Count;
		Update_Previews();
    }
	private void _on_ready_player_4_button_down()
    {
        p4Ready = !p4Ready;
		CheckBothReady();
    }
	private void CheckBothReady()
    {
        startButton.Disabled = !(p1Ready && p2Ready && p3Ready && p4Ready);
    }
	public void _on_start_button_down()
    {
		GameData.Player1Character = p1Index;
		GameData.Player2Character = p2Index;
		GameData.Player3Character = p3Index;
		GameData.Player4Character = p4Index;

		if(Multiplayer.IsServer())
        	Lobby.Instance.Rpc(Lobby.MethodName.GoToGameScene);
    }
}
