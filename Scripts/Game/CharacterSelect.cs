using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterSelect : Control
{
	// Called when the node enters the scene tree for the first time.

	private List<Texture2D> characters = new List<Texture2D>();
	private int p1Index = 0;
	private int p2Index = 0;
	private bool p1Ready = false;
	private bool p2Ready = false;
	private TextureRect p1Preview;
	private TextureRect p2Preview;
	private Button startButton;
	public override void _Ready()
    {
        characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cyberpunk.png"));
		characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_knight.png"));
		characters.Add(ResourceLoader.Load<Texture2D>("Assets//CharacterPreview//player_cat.png"));

		p1Preview = GetNode<TextureRect>("MarginContainer/HBoxContainer/Player1/CharacterPreview");
		p2Preview = GetNode<TextureRect>("MarginContainer/HBoxContainer/Player2/CharacterPreview");
		startButton = GetNode<Button>("StartButton");

		UpdatePreviews();
    }
	private void UpdatePreviews()
    {
        p1Preview.Texture = characters[p1Index];
		p2Preview.Texture = characters[p2Index];
    }
	private void _on_links_player_1_button_down()
    {
        if (p1Ready) 
			return;
		p1Index = (p1Index - 1 + characters.Count) % characters.Count;
		UpdatePreviews();
    }
	private void _on_rechts_player_1_button_down()
    {
       if (p1Ready) 
			return;
		p1Index = (p1Index + 1) % characters.Count;
		UpdatePreviews();
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
		UpdatePreviews();
    }
	private void _on_rechts_player_2_button_down()
    {
       if (p2Ready) 
			return;
		p2Index = (p2Index + 1) % characters.Count;
		UpdatePreviews();
    }
	private void _on_ready_player_2_button_down()
    {
        p2Ready = !p2Ready;
		CheckBothReady();
    }
	private void CheckBothReady()
    {
        startButton.Disabled = !(p1Ready && p2Ready);
    }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public void _on_start()
    {
        GetTree().ChangeSceneToFile("res://Scenes/map.tscn");
    }
}
