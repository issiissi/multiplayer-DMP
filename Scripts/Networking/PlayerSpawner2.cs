using System;
using Godot;
using Godot.Collections; // Required for the Array
//using Godot.Collections.Array;
public partial class PlayerSpawner2 : MultiplayerSpawner
{
    [Export]
    public PackedScene[] playerCharacter;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, nameof(CustomMultiplayerSpawn));
    }


    public void SpawnPlayers()
    {
        int spawnCounter = 0;
        foreach (long playerID in Lobby.Instance._players.Keys)
        {
            // FIX 1: Pass BOTH index and ID to the spawn function
            int characterIndex = Int32.Parse(Lobby.Instance._players[playerID]["CharacterID"]);

            // We pack the data into a Godot Array to send multiple values
            Godot.Collections.Array spawnData = new Godot.Collections.Array { characterIndex, playerID, spawnCounter };

            // Triggers CustomMultiplayerSpawn on Server AND Client
            Spawn(spawnData);
            spawnCounter++;
        }
    }

    // FIX 2: Accept Variant (which contains our Array)
    private Node CustomMultiplayerSpawn(Variant data)
    {
        // Unpack the data
        Godot.Collections.Array spawnData = (Godot.Collections.Array)data;
        int characterIndex = (int)spawnData[0];
        long playerID = (long)spawnData[1];
        int spawnIndex = (int)spawnData[2];

        GD.Print($"[SPAWNER] {Multiplayer.GetUniqueId()} spawning player for ID: {playerID}");

        // Instantiate
        PackedScene scene = playerCharacter[characterIndex];
        Node node = scene.Instantiate();

        CharacterBody2D characterBody2D = (CharacterBody2D)node;
        Node2D parent = (Node2D)GetNode(SpawnPath);
        characterBody2D.GlobalPosition = parent.ToLocal(GetSpawnPositionWorld(spawnIndex));

        node.Name = playerID.ToString();
        node.SetMultiplayerAuthority((int)playerID);

        return node;
    }


    private Node FindNodeByName(Node parent, string name)
    {
        if (parent.Name == name)
            return parent;

        foreach (Node child in parent.GetChildren())
        {
            Node found = FindNodeByName(child, name);
            if (found != null)
                return found;
        }

        return null; // Not found
    }

    private Vector2 GetSpawnPositionWorld(int index)
    {

        Marker2D node = (Marker2D)FindNodeByName(GetTree().Root, $"SpawnPoint_{index + 1}");
        return node.GlobalPosition;
    }
}