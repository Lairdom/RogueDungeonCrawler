using Godot;
using System;

public partial class passiveskillpickup : Node3D
{
	private string skillName;
    private string description;

	public void Setup(string name, string desc)
    {
        skillName = name;
        description = desc;
    }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
