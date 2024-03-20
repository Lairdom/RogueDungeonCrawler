using Godot;
using System;
using System.Diagnostics;

public partial class WebAttack : Area3D
{
	public EnemyBoss boss;
	public int damage;
	float timer = 0;
	Player player;

	private void OnHit(Node3D body) {
		if (body.Name == "Player" || body.Name == "ShieldCollider") {
			boss.caughtPlayer = true;
		}
		
		QueueFree();
	}

	public override void _Ready() {
		player = GetNodeOrNull<Player>("/root/World/Player");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		Vector3 direction = Transform.Basis * Vector3.Forward;
		Position += direction * 5 * delta;
		timer += delta;
		
		Scale = Scale.Lerp(new Vector3(3, 3, 3), timer/5);
		if (timer > 5)
			QueueFree();
	}
}
