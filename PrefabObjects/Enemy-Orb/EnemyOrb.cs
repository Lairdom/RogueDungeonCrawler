using Godot;
using System;
using System.Diagnostics;

public partial class EnemyOrb : CharacterBody3D
{
	Node3D player = default;
	Vector3 playerDirection;
	bool alive = true;
	float playerDistance;
	PackedScene bulletScene = ResourceLoader.Load("res://PrefabObjects/Enemy-Orb/Bullet.tscn") as PackedScene;
	float shootTimer = 0;
	bool shooting = false;
	Node3D root = default;
	Node3D statHandler = default;

	// Signaali joka saadaan kun health putoaa alle 0
	public void OnDeath(float deathDelayTime) {
		// Death animations
		QueueFree();
	}

	public void TakeDamage(int dmg) {
		Debug.Print("Took "+dmg+" damage");
		statHandler.CallDeferred("ChangeHealth", dmg);
	}

	void SpawnBullet() {
		Node3D bulletInstance = (Node3D) bulletScene.Instantiate();
		bulletInstance.Position = Position;
		bulletInstance.Rotation = Rotation;
		root.AddChild(bulletInstance);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		statHandler = GetNode<Node3D>("EnemyHandler");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Node3D>("/root/World/Player");
		if (player != null) {
			Debug.Print("Player detected");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (player != null) {
			playerDirection = player.GlobalPosition - GlobalPosition;
			playerDirection = playerDirection.Normalized();
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			LookAt(player.GlobalPosition);
			if (playerDistance < 10 && shootTimer <= 0) {
				shooting = true;
				SpawnBullet();
				shootTimer = 2;
			}
			shootTimer -= delta;
			if (shootTimer <= 0)
				shooting = false;
		}
	}
}
