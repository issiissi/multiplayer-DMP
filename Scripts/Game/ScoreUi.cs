using Godot;
using System;
using System.ComponentModel;

public partial class ScoreUi : CanvasLayer
{
	[Export] public NodePath ResultLabelPath = "PanelContainer/MarginContainer/Label";
	private Label label;
	public override void _Ready()
	{
		label = GetNode<Label>(ResultLabelPath);
		Visible = false;
	}

	public void show_Results(string text)
	{
		label.Text = text;
		Visible = true;
	}
}
