using Godot;
using System;

public partial class PlayerSpawner : Node
{
	private Marker2D SpawnPoint_1;
	private Marker2D SpawnPoint_2;
	private Marker2D SpawnPoint_3;
	public override void _Ready()
    {
        SpawnPoint_1 = GetNode<Marker2D>("../SpawnPoints/SpawnPoint_1");
		SpawnPoint_2 = GetNode<Marker2D>("../SpawnPoints/SpawnPoint_2");
		SpawnPoint_3 = GetNode<Marker2D>("../SpawnPoints/SpawnPoint_3");

		SpawnLocalPlayer();
    }

	private void SpawnLocalPlayer()
    {
        string[] characterScenes =
        {
            "res://Scenes/Scenes_Player/player.tscn",
			"res://Scenes/Scenes_Player/player_2.tscn",
			"res://Scenes/Scenes_Player/player_3.tscn"
        };

		int id = Multiplayer.GetUniqueId();

		int chosenCharacter = Lobby.Instance.GetChoiseFor(id);

		chosenCharacter = Mathf.Clamp(chosenCharacter, 0, characterScenes.Length - 1);

		GD.Print($"[Spawner] id={id} chosenCharacter={chosenCharacter}");

		var packed = GD.Load<PackedScene>(characterScenes[chosenCharacter]);
		var player = packed.Instantiate<Node2D>();

		player.SetMultiplayerAuthority(id);
		AddChild(player);

		player.GlobalPosition = id switch
		{
			1 => SpawnPoint_1.GlobalPosition,
			2 => SpawnPoint_2.GlobalPosition,
			3 => SpawnPoint_3.GlobalPosition,
			_ => SpawnPoint_1.GlobalPosition
		};
	}
}
