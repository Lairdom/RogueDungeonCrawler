using Godot;
using System;
using System.Diagnostics;

public partial class WebAttack : Area3D
{
	public EnemyBoss boss;
	public int damage;
	float timer = 0;
	Player player;
	bool playerHit = false;

	private void OnHit(Node3D body) {
		if (body.Name == "Player" || body.Name == "ShieldCollider") {
			playerHit = true;
			player.webbed = true;
			boss.caughtPlayer = true;
		}
		else if (playerHit == false)
			QueueFree();
	}

	public override void _Ready() {
		player = GetNodeOrNull<Player>("/root/World/Player");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (playerHit) {
			Position = new Vector3(player.Position.X, player.Position.Y-0.3f, player.Position.Z);
			RotationDegrees = new Vector3(-90, 0, 0);
			Scale = new Vector3(3, 3, 3);
			if (player.webbed == false)
				QueueFree();
		}
		else {
			Vector3 direction = Transform.Basis * Vector3.Forward;
			Position += direction * 5 * delta;
			timer += delta;
			Scale = Scale.Lerp(new Vector3(3, 3, 3), timer/5);
			if (timer > 5)
				QueueFree();
		}
	}
}
