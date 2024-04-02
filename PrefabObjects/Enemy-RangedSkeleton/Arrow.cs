using Godot;
using System;

public partial class Arrow : RigidBody3D
{
	public int damage;
	float timer = 5;
	Player player;

	private void OnHit(Node3D body) {
		if (body.Name == "Player") {
			player.PlayerTakeDamage(damage);
		}
		else if (body.Name == "ShieldCollider") {
			player.ShieldHit();
		}
		
		QueueFree();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		player = GetNodeOrNull<Player>("/root/World/Player");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		Vector3 direction = (Transform.Basis * Vector3.Forward).Normalized();
		Vector3 tempVelocity = direction * 5;
		LinearVelocity = tempVelocity;
		timer -= delta;
		if (timer <= 0)
			QueueFree();
	}
}
