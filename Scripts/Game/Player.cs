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
	public override void _Ready()
	{
		anim_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		nameLabel = GetNode<Label>("Name_InGame");

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

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
			return;

		Vector2 velocity = Velocity;

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

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
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
