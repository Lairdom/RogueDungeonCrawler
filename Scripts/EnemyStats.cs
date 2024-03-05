using Godot;
using System;

public partial class EnemyStats : Node3D
{
	[Export] int maxHealth;
	[Export] int currentHealth;
	[Export(PropertyHint.Flags, "Slashing,Piercing,Bludgeoning")] int weakness;
	

	// Start. Loaded once
	public override void _Ready() {

	}

	// Updates every frame
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;

	}
}
