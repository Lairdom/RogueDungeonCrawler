using Godot;
using System;
using System.Diagnostics;

public partial class EnemyOrb : CharacterBody3D
{
	[Export] float firingDelay;
	GameManager GM;
	Player player = default;
	Vector3 playerDirection;
	bool alive = true;
	float playerDistance;
	float shootTimer = 0;
	bool shooting = false;
	Node3D root = default;
	EnemyStats statHandler = default;
	AudioStreamPlayer3D audioSource = default;
	AudioStreamOggVorbis hitSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyHit1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis deathSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyDeath1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis fireBall = ResourceLoader.Load("res://Audio/SoundEffects/EnemyFireball1.ogg") as AudioStreamOggVorbis;
	PackedScene bulletScene = ResourceLoader.Load("res://PrefabObjects/Enemy-Orb/Bullet.tscn") as PackedScene;

	// Signaali joka saadaan kun health putoaa alle 0
	public async void OnDeath(float deathDelayTime) {
		alive = false;
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
