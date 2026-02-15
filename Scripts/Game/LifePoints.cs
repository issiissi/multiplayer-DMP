using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;

public partial class LifePoints : CanvasLayer
{
	// Assets
	[Export] public Texture2D HeartFull;
	[Export] public Texture2D HeartEmpty;
	[Export] public StringName LifeLostAnimName = "death";

	// Nodes
	[Export] public NodePath HeartsContainerPath = "Hearts";
	private HBoxContainer heartsBox;

	// Referenzen auf die UI-Elemente
	private List<TextureRect> icons = new();
	private List<AnimatedSprite2D> fxSprites = new();

	// Zustände
	private int currentLifes = -1;
	private int pendingLifes = -1;

	public override void _Ready()
	{
		AddToGroup("LifePoints");

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
			lossFX.Stop();
			lossFX.Frame = 0;

			int index = i;
			lossFX.AnimationFinished += () => OnLossFXFinished(index);
			i++;
		}

		ResetLifes(3);
	}

	// Anzeige der Leben zu Beginn oder nach Respawn
	public void ResetLifes(int remaining)
	{
		remaining = Mathf.Clamp(remaining, 0, icons.Count);
		currentLifes = remaining;
		pendingLifes = -1;

		ApplyIcons(currentLifes);

		for(int i = 0; i < fxSprites.Count; i++)
		{
			fxSprites[i].Stop();
			fxSprites[i].Frame = 0;
			fxSprites[i].Visible = false;
		}	
	}

	// Lebensverlust
	public void OnLifesChanged(int remaining)
	{
		remaining = Mathf.Clamp(remaining, 0, icons.Count);

		if(remaining == currentLifes || remaining == pendingLifes)
			return;

		if(remaining < 0 || remaining > currentLifes)
		{
			currentLifes = remaining;
			ApplyIcons(currentLifes);
			return;
		}

		int prevlifes = currentLifes;
		pendingLifes = remaining;

		int index = Mathf.Clamp(icons.Count - prevlifes, 0, icons.Count - 1);

		var lossFX = fxSprites[index];
		lossFX.Stop();
		lossFX.Frame = 0;
		lossFX.Visible = true;
		lossFX.Play(LifeLostAnimName);
	}

	private void OnLossFXFinished(int index)
	{
		if(index >= 0 && index < fxSprites.Count)
		{
			fxSprites[index].Stop();
			fxSprites[index].Frame = 0;
			fxSprites[index].Visible = false;
		}
		
		if(pendingLifes >= 0)
		{
			currentLifes = pendingLifes;
			pendingLifes = -1;
			ApplyIcons(currentLifes);
		}
	}

	// UI-Update der Herzen
	private void ApplyIcons(int lifes)
	{
		for(int i = 0; i < icons.Count; i++)
		{
			bool isFull = i >= icons.Count - lifes;
			icons[i].Texture = isFull ? HeartFull : HeartEmpty;
		}
	}
}
