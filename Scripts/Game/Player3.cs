using Godot;
using System;

public partial class Player_3 : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	private AnimatedSprite2D anim_sprite;
	public override void _Ready()
	{
		anim_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
			anim_sprite.Play("jump");
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;

			if (direction.X != 0)
			{
				anim_sprite.FlipH = direction.X < 0;
			}
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
