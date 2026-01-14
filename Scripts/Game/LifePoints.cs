using Godot;
using System;
using System.Collections.Generic;

public partial class LifePoints : CanvasLayer
{
	[Export] public Texture2D HeartFull;
	[Export] public Texture2D HeartEmpty;
	[Export] public StringName LifeLostAnimName = "death";

	[Export] public NodePath HeartsContainerPath = "Hearts";
	private HBoxContainer heartsBox;

	private List<TextureRect> icons = new();
	private List<AnimatedSprite2D> fxSprites = new();

	private int currentLives = -1;
	private int pendingLives = -1;

	public override void _Ready()
	{
		AddToGroup("LivePoints");

		heartsBox = GetNode<HBoxContainer>(HeartsContainerPath);

		icons.Clear();
		fxSprites.Clear();

		int i = 0;
		foreach(var child in heartsBox.GetChildren())
		{
			if(child is not Control slot)
				continue;
			
			var icon = slot.GetNode<TextureRect>("Icon");
			var lossFX = slot.GetNode<AnimatedSprite2D>("LossFX");

			icons.Add(icon);
			fxSprites.Add(lossFX);

			lossFX.Visible = false;

			int index = i;
			lossFX.AnimationFinished += () => OnLossFXFinished(index);

			i++;
		}

		ResetLives(icons.Count);
	}

	public void ResetLives(int lives)
	{
		currentLives = Mathf.Clamp(lives, 0, icons.Count);
		pendingLives = -1;
		ApplyIcons(currentLives);

		for(int i = 0; i < fxSprites.Count; i++)
			fxSprites[i].Visible = false;
	}

	public void OnLivesChanged(int remaining)
	{
		remaining = Mathf.Clamp(remaining, 0, icons.Count);

		if(currentLives < 0 || remaining >= currentLives)
		{
			currentLives = remaining;
			ApplyIcons(currentLives);
			return;
		}

		int index = Mathf.Clamp(remaining, 0, icons.Count -1);

		pendingLives = remaining;

		var lossFX = fxSprites[index];
		lossFX.Visible = true;
		lossFX.Frame = 0;
		lossFX.Play(LifeLostAnimName);
	}

	private void OnLossFXFinished(int index)
	{
		if(index >= 0 && index < fxSprites.Count)
			fxSprites[index].Visible = false;
		
		if(pendingLives >= 0)
		{
			currentLives = pendingLives;
			pendingLives = -1;
			ApplyIcons(currentLives);
		}
	}

	private void ApplyIcons(int lives)
	{
		for(int i = 0; i < icons.Count; i++)
			icons[i].Texture = (i < lives) ? HeartFull : HeartEmpty;
	}
}
