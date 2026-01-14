using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	private AnimatedSprite2D anim_sprite;
	private Label nameLabel;

	//set playername on spawn
	public string PlayerName = "Unbenannt";

	// Respawn System
	private Vector2 respawn_Point;
	private bool has_respawn_Point = false;

	// Klettern
	[Export] public float ClimbSpeed = 220f;
	private bool onLadder = false;

	// Life Mechanic
	private bool eliminated = false;

	//  Game Over
	private bool game_Over = false;

	private Vector2 spawn_Point;
	private bool has_Spawn_Point = false;

	public override void _Ready()
	{
		anim_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		nameLabel = GetNode<Label>("Name_InGame");

		AddToGroup("Players");

		// SETUP SYNCHRONIZATION VIA CODE
		var sync = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		var config = new SceneReplicationConfig();

		// Sync Position: Mode.Always means real-time synchronization
		NodePath posPath = new NodePath(":position");
		config.AddProperty(posPath);
		config.PropertySetReplicationMode(posPath, SceneReplicationConfig.ReplicationMode.Always);

		// Sync Velocity (helps with smooth animation/physics)
		NodePath velPath = new NodePath(":velocity");
		config.AddProperty(velPath);
		config.PropertySetReplicationMode(velPath, SceneReplicationConfig.ReplicationMode.Always);

		if(!has_Spawn_Point)
		{
			spawn_Point = GlobalPosition;
			has_Spawn_Point = true;
		}

		if(!has_respawn_Point)
			SetRespawnPoint(spawn_Point, silent: true);

		sync.ReplicationConfig = config;

		Initialize();

		if(!has_respawn_Point)
			SetRespawnPoint(GlobalPosition, silent: true);
	}

	public void Initialize()
	{
		long id = GetMultiplayerAuthority();
		GD.Print($"[Player] has multiplayer Authority: {id}");
		TrySetName(id);
		SetProcessInput(IsMultiplayerAuthority());

		if (!IsMultiplayerAuthority())
		{
			//Remove the camera 
			Camera2D cam = GetNodeOrNull<Camera2D>("Camera2D");
			if (cam != null)
			{
				cam.QueueFree(); // Remove it safely
			}
		}
	}

	private void TrySetName(long id)
	{
		string name = Lobby.Instance._players[id]["Name"];
		nameLabel.Text = name;
	}

	public void SetRespawnPoint(Vector2 worldPos, bool silent = false)
	{
		respawn_Point = worldPos;
		has_respawn_Point = true;

		if(!silent)
			GD.Print($"[Checkpoint] Respawnpoint saved 💾 -> {respawn_Point}");
	}

	public void Respawn()
	{
		if(!has_respawn_Point)
			SetRespawnPoint(GlobalPosition, silent: true);
		
		Rpc(nameof(RpcApplyRespawn), respawn_Point);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcApplyRespawn(Vector2 worldPos)
	{
		Velocity = Vector2.Zero;
		GlobalPosition = worldPos;

		if(anim_sprite != null)
			anim_sprite.Play("idle");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RpcClientRespawn()
	{
		if(!Multiplayer.IsServer() && Multiplayer.GetRemoteSenderId() != 1)
			return;
		if(!IsMultiplayerAuthority())
			return;
		
		Respawn();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RpcClientSetEliminated(bool value)
	{
		if(!Multiplayer.IsServer() && Multiplayer.GetRemoteSenderId() != 1)
			return;
		
		eliminated = value;

		if(eliminated)
		{
			Velocity = Vector2.Zero;
			anim_sprite.Play("idle");
		}
	}

	public void SetOnLadder(bool on)
	{
		onLadder = on;
		GD.Print($"[Player] onLadder = {onLadder}");

		if (on)
			Velocity = new Vector2(Velocity.X, 0);
	}

	public void SetGameOver(bool over)
	{
		game_Over = over;
		if(over)
		{
			Velocity = Vector2.Zero;
			anim_sprite?.Play("idle");
		}
	}

	public void PlayAgainNewRound()
	{
		eliminated = false;
		game_Over = false;
		onLadder = false;

		if(!IsMultiplayerAuthority())
			return;
		SetGameOver(false);

		onLadder = false;
		Velocity = Vector2.Zero;

		if(!has_Spawn_Point)
		{
			spawn_Point = GlobalPosition;
			has_Spawn_Point = true;
		}

		GlobalPosition = spawn_Point;
		SetRespawnPoint(spawn_Point, silent: true);

		anim_sprite.Play("idle");
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
			return;
		if(game_Over)
			return;
		if(eliminated)
			return;
		
		Vector2 velocity = Velocity;
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (onLadder)
		{
			velocity.Y = direction.Y * ClimbSpeed;

			if(Mathf.Abs(direction.Y) < 0.01f)
				velocity.Y = 0;
			
			if(Input.IsActionJustPressed("jump"))
			{
				onLadder = false;
				velocity.Y = JumpVelocity;
			}
		}

		else
		{
			// Add the gravity.
			if (!IsOnFloor())
			{
				velocity += GetGravity() * (float)delta;
			}

			// Handle Jump.
			if (Input.IsActionJustPressed("jump") && IsOnFloor())
			{
				velocity.Y = JumpVelocity;
				anim_sprite.Play("jump");
			}
		}

		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
			anim_sprite.FlipH = direction.X < 0;

		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}
		if (!IsOnFloor())
		{
			anim_sprite.Play("jump");
		}
		else
		{
			if (direction.X != 0)
				anim_sprite.Play("walk");
			else
				anim_sprite.Play("idle");
		}


		Velocity = velocity;
		MoveAndSlide();
	}
}
