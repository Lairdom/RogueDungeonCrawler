using Godot;
using System;
using System.Diagnostics;

public partial class EnemyStats : Node3D
{
	[Export] int maxHealth;
	[Export] public int currentHealth;
	[Export] public int damage;
	[Export] public float movementSpeed;
	[Export] public float aggroRange;
	[Export(PropertyHint.Flags, "Slashing,Piercing,Bludgeoning")] int weakness;
	[Signal] public delegate void DeathSignalEventHandler(float deathDelayTime);
	
	public void ChangeHealth(int amount) {
		currentHealth -= amount;

		// Palautetaan true jos health on yli 0 ja false jos alle
		if (currentHealth <= 0) {
			EmitSignal(SignalName.DeathSignal, 0.5f);
		}
	}

	public async void SetStats() {
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		// If Large version of monster
		if (GetParent<Node3D>().Scale.Y > 1) {
			maxHealth *= 5;
			currentHealth = maxHealth;
			damage *= 3;
			movementSpeed *= 2;
			aggroRange *= 2f;
		}
		// If baby version of monster
		else if (GetParent<Node3D>().Scale.Y < 1) {
			maxHealth = (int)(maxHealth * 0.5f);
			currentHealth = maxHealth;
			damage = (int)(damage * 0.75f);
			movementSpeed = (int)(movementSpeed * 0.5f);
		}
	}

	// Start. Loaded once
	public override void _Ready() {
		SetStats();
	}

	// Updates every frame
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;

	}
}
