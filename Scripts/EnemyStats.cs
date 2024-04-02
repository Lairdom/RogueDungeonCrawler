using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;


public partial class EnemyStats : Node3D
{
	[Export] int maxHealth;
	[Export] public int currentHealth;
	[Export] public int damage;
	[Export] public float attackSpeed;
	[Export] public float movementSpeed;
	[Export] public float aggroRange;
	[Export(PropertyHint.Flags, "Slashing,Piercing,Bludgeoning")] int weakTo;
	public List<string> weaknesses = new List<string>();
	[Export(PropertyHint.Flags, "Slashing,Piercing,Bludgeoning")] int immuneTo;
	public List<string> immunities = new List<string>();
	[Signal] public delegate void DeathSignalEventHandler(float deathDelayTime);
	public bool isAlive = true;
	
	public void ChangeHealth(int amount) {
		currentHealth -= amount;

		// Palautetaan true jos health on yli 0 ja false jos alle
		if (currentHealth <= 0) {
			EmitSignal(SignalName.DeathSignal, 0.5f);
		}
	}

	public async void SetStats() {
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		// Set the weakTo flags as strings into weaknesses
		if (weakTo == 1 || weakTo == 3 || weakTo == 5 || weakTo == 7)
			weaknesses.Add("Slashing");
		if (weakTo == 2 || weakTo == 3 || weakTo == 6 || weakTo == 7)
			weaknesses.Add("Piercing");
		if (weakTo == 4 || weakTo == 5 || weakTo == 6 || weakTo == 7)
			weaknesses.Add("Bludgeoning");

		// Set the immuneTo flags as strings into immunities
		if (immuneTo == 1 || immuneTo == 3 || immuneTo == 5 || immuneTo == 7)
			immunities.Add("Slashing");
		if (immuneTo == 2 || immuneTo == 3 || immuneTo == 6 || immuneTo == 7)
			immunities.Add("Piercing");
		if (immuneTo == 4 || immuneTo == 5 || immuneTo == 6 || immuneTo == 7)
			immunities.Add("Bludgeoning");

		// If Large version of monster
		if (GetParent<Node3D>().Scale.Y > 1) {
			maxHealth *= 5;
			currentHealth = maxHealth;
			damage *= 3;
			movementSpeed *= 3;
			aggroRange *= 3f;
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
}
