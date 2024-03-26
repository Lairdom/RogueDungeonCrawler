using Godot;
using System;

public partial class StartHelpExitMenu : CanvasLayer {

	[ExportGroup("Buttons")]
	[ExportSubgroup("Button Set")]
	[Export] public TextureButton start;
	[Export] public TextureButton help;
	[Export] public TextureButton exit;
	[Export] public Label helpLabel;

	public override void _Ready() {
		start.Connect("pressed", new Callable(this, nameof(OnStartPressed)));
		help.Connect("pressed", new Callable(this, nameof(OnHelpPressed)));
		exit.Connect("pressed", new Callable(this, nameof(OnExitPressed)));
	}

	private void OnStartPressed() {
		// Starts the game
		var nextScene = (PackedScene)ResourceLoader.Load("res://Scenes/world.tscn");
		GetTree().ChangeSceneToPacked(nextScene);
	}

	private void OnHelpPressed() {
		helpLabel.Visible = true;
	}

	private void OnExitPressed() {
		// Closes the game
		GetTree().Quit();
	}
	
	public override void _Process(double delta)
	{
	}
}
