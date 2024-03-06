using Godot;
using System;
using System.Diagnostics;

public partial class EnemyOrb : CharacterBody3D
{
	[Export] float firingDelay;
	GameManager GM;
	Node3D player = default;
	Vector3 playerDirection;
	bool alive = true;
	float playerDistance;
	PackedScene bulletScene = ResourceLoader.Load("res://PrefabObjects/Enemy-Orb/Bullet.tscn") as PackedScene;
	float shootTimer = 0;
	bool shooting = false;
	Node3D root = default;
	EnemyStats statHandler = default;

	// Signaali joka saadaan kun health putoaa alle 0
	public void OnDeath(float deathDelayTime) {
		alive = false;
		// Death animations
		QueueFree();
	}

	public void TakeDamage(int dmg) {
		Debug.Print("Took "+dmg+" damage");
		statHandler.CallDeferred("ChangeHealth", dmg);
	}

	void SpawnBullet() {
		Bullet bulletInstance = (Bullet) bulletScene.Instantiate();
		bulletInstance.damage = statHandler.damage;
		bulletInstance.Position = Position;
		bulletInstance.Rotation = Rotation;
		root.AddChild(bulletInstance);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		statHandler = GetNode<EnemyStats>("EnemyHandler");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (player != null && alive) {
			playerDirection = player.GlobalPosition - GlobalPosition;
			playerDirection = playerDirection.Normalized();
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			LookAt(player.GlobalPosition);
			if (playerDistance < 5 && shootTimer <= 0 && GM.playerAlive) {
				shooting = true;
				SpawnBullet();
				shootTimer = firingDelay;
			}
			shootTimer -= delta;
			if (shootTimer <= 0)
				shooting = false;
		}
	}
}
