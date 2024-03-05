using Godot;
using System;
using System.Diagnostics;

public partial class Bullet : Area3D
{
	public int damage = 20;
	float timer = 5;
	GameManager GM = default;
	

	private void OnHit(Node3D body) {
		if (body.Name == "Player") {
			Debug.Print("Player hit.");
			GM.ChangePlayerHealth(-damage);
			Debug.Print("Health: "+GM.playerHealth);
		}
		else if (body.Name == "ShieldCollider") {
			Debug.Print("Shield hit. *play ricochet SFX");
		}
		//Free(); 		// Deletes node immediately
		QueueFree();	// Deletes Node after all its DeferredCalls have ended
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		Vector3 direction = Transform.Basis * Vector3.Forward;
		Position += direction * 5 * delta;
		timer -= delta;
		if (timer <= 0)
			QueueFree();
	}
}
