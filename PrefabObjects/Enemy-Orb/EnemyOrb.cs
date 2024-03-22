using Godot;
using System;
using System.Diagnostics;

public partial class EnemyOrb : CharacterBody3D
{
	GameManager GM;
	Player player = default;
	Vector3 playerDirection;
	float playerDistance;
	float shootTimer = 0;
	float firingDelay;
	Node3D root = default;
	public EnemyStats statHandler = default;
	AudioStreamPlayer3D audioSource = default;
	AudioStreamOggVorbis hitSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyHit1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis deathSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyDeath1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis fireBall = ResourceLoader.Load("res://Audio/SoundEffects/EnemyFireball1.ogg") as AudioStreamOggVorbis;
	PackedScene bulletScene = ResourceLoader.Load("res://PrefabObjects/Enemy-Orb/Bullet.tscn") as PackedScene;

	// Signaali joka saadaan kun health putoaa alle 0
	public async void OnDeath(float deathDelayTime) {
		statHandler.isAlive = false;
		GM.CheckAllEnemiesDefeated();
		PlayAudioOnce(deathSound, -20);
		// Death animations
		await ToSignal(GetTree().CreateTimer(deathDelayTime), "timeout");
		QueueFree();
	}

	// Signaali joka saadaan kun vihollinen ottaa damagea
	public void TakeDamage(int dmg) {
		statHandler.ChangeHealth(dmg);
		if (statHandler.currentHealth > 0) {
			PlayAudioOnce(hitSound, -20);
		}
	}

	// Luodaan ammus
	private void SpawnBullet() {
		Bullet bulletInstance = (Bullet) bulletScene.Instantiate();
		bulletInstance.damage = statHandler.damage;
		bulletInstance.Position = Position;
		bulletInstance.Rotation = Rotation;
		root.AddChild(bulletInstance);
		PlayAudioOnce(fireBall, -20);
	}

	private void PlayAudioOnce(AudioStreamOggVorbis clip, int volume) {
		audioSource.Stream = clip;
		audioSource.VolumeDb = volume;
		audioSource.Play();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		statHandler = GetNode<EnemyStats>("EnemyHandler");
		audioSource = GetNode<AudioStreamPlayer3D>("AudioPlayer");
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		firingDelay = statHandler.attackSpeed;
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (GM.playerAlive && statHandler.isAlive) {
			playerDirection = player.GlobalPosition - GlobalPosition;
			playerDirection = playerDirection.Normalized();
			playerDistance = Position.DistanceTo(player.GlobalPosition);
			LookAt(player.GlobalPosition);

			// Raycast pelaajaa kohti jotta tiedetään onko bossilla näköyhteys pelaajaan
			var spaceState = GetWorld3D().DirectSpaceState;
			var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, player.GlobalPosition);
			var result = spaceState.IntersectRay(query);
			Node3D hitNode = (Node3D) result["collider"];
			bool seesPlayer = hitNode.Name == "Player";

			if (playerDistance < statHandler.aggroRange && seesPlayer && shootTimer > firingDelay) {
				SpawnBullet();
				shootTimer = 0;
			}
			if (shootTimer < firingDelay)
				shootTimer += delta;
		}
	}
}
