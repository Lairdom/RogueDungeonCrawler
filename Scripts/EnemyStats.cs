using Godot;
using System;
using System.Diagnostics;

public partial class EnemyStats : Node3D
{
	[Export] int maxHealth;
	[Export] int currentHealth;
	[Export] public int damage;
	[Export(PropertyHint.Flags, "Slashing,Piercing,Bludgeoning")] int weakness;
	[Signal] public delegate void DeathSignalEventHandler(float deathDelayTime);
	
	public void ChangeHealth(int amount) {
		currentHealth -= amount;

		// Palautetaan true jos health on yli 0 ja false jos alle
		if (currentHealth <= 0) {
			EmitSignal(SignalName.DeathSignal, 0.1f);
		}
	}

	// Start. Loaded once
	public override void _Ready() {

	}

	// Updates every frame
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;

	}
}
