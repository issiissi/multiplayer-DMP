using Godot;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public partial class ScoreUi : CanvasLayer
{
	[Export] public NodePath ResultLabelPath = "PanelContainer/MarginContainer/CenterContainer/VBoxContainer/Label";
	[Export] public NodePath PlayAgainButtonPath = "ScoreUI/PanelContainer/MarginContainer/CenterContainer/VBoxContainer/PlayAgain";
	[Export] public NodePath LeaveButtonPath = "ScoreUI/PanelContainer/MarginContainer/CenterContainer/VBoxContainer/Leave";
	[Export] public NodePath UIManagerPath = "/root/Lobby/UI Manager";
	
	private Label label;
	private Button playAgain;
	private Button leave;
	public UIManager ui;

	private string resultText = "";
	private bool pressedPlayAgain = false;
	public override void _Ready()
	{
		AddToGroup("ScoreUIs");

		label = GetNode<Label>(ResultLabelPath);
		playAgain = GetNode<Button>(PlayAgainButtonPath);
		leave = GetNode<Button>(LeaveButtonPath);

		ui = GetNodeOrNull<UIManager>(UIManagerPath);

		playAgain.Pressed += _on_play_again_pressed;
		leave.Pressed += _on_leave_pressed;

		if(Lobby.Instance != null)
		{
			Lobby.Instance.PlayAgainProgress += OnPlayAgainProgress;
			Lobby.Instance.PlayAgainExpired += OnPlayAgainExpired;
			Lobby.Instance.GameRestarted += OnGameRestarted;
		}

		Visible = false;
	}

	public void show_Results(string text)
	{
		resultText = text;
		
		if(label != null)
			label.Text = resultText;

		pressedPlayAgain = false;
		if(playAgain != null)
			playAgain.Disabled = false;
		
		Visible = true;
	}

	public void HideResults()
	{
		Visible = false;
		pressedPlayAgain = false;

		if(playAgain != null)
			playAgain.Disabled = false;
	}

	public void _on_play_again_pressed()
	{
		playAgain.Disabled = true;
		
		if(Multiplayer.IsServer())
			Lobby.Instance.RequestPlayAgain();
		else
			Lobby.Instance.RpcId(1, nameof(Lobby.RequestPlayAgain));
	}

	public void _on_leave_pressed()
	{
		HideResults();

		Lobby.Instance.ClearGameWorldLocal();

		if(ui != null)
			ui.LeaveLobby();
		else
		Lobby.Instance.RemoveMultiplayerPeer();
	}

	private void OnPlayAgainProgress(int pressed, int total, float secondsLeft)
	{
		if (!Visible || label == null)
			return;

		int sec = Mathf.CeilToInt(secondsLeft);
		label.Text = $"{resultText}\n\nPlay Again Votes: {pressed}/{total}  (noch {sec}s)";
	}

	private void OnPlayAgainExpired()
	{
		if(!Visible || label == null)
			return;
		
		pressedPlayAgain = false;
		if(playAgain != null)
			playAgain.Disabled = false;

		label.Text = $"{resultText}\n\n⏱️ Nicht alle haben gedrückt. Try again.";
	}

	private void OnGameRestarted()
	{
		HideResults();
	}
}
