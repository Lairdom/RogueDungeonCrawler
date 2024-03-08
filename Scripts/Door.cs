using Godot;
using System;
using System.Diagnostics;

public partial class Door : Node3D
{
	
	private AnimationPlayer animationPlayer;
	private bool isOpening = false; // Track if the door is currently opening
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Examine"))
		{
			// Toggle the door animation
			if (!animationPlayer.IsPlaying())
			{
				animationPlayer.Play("open");
			}
			else
			{
				animationPlayer.Stop();
				animationPlayer.Play("close");
			}
		}
	}
}
