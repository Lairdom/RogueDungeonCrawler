using Godot;
using System;
using System.Diagnostics;

public partial class EnemySpider : CharacterBody3D
{
	GameManager GM;
	Player player;
	Node3D root;
	Vector3 playerDirection;
	EnemyStats statHandler;
	AudioStreamPlayer3D audioSource;
	bool isAlive = true;
	float playerDistance;
	NavigationAgent3D pathFinder;
	Vector3 movementTarget;
	float moveSpeed;
	bool playerDetected = false;
	float attackTimer;
	AudioStreamOggVorbis hitSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyHit1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis deathSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyDeath1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis spiderAttack = ResourceLoader.Load("res://Audio/SoundEffects/EnemyFireball1.ogg") as AudioStreamOggVorbis;
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	// Pathfinder set/get target position for movement
	public Vector3 MovementTarget {
		get { return pathFinder.TargetPosition; }
		set { pathFinder.TargetPosition = value; }
	}

	// Set Movement target
	private async void MovementSetup() {
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		MovementTarget = movementTarget;
	}

	// Hämähäkin melee hyökkäys
	private async void SpiderAttack() {
		// SpiderAttack animations
		float animDuration = 1;
		PlayAudioOnce(spiderAttack, -20);
		await ToSignal(GetTree().CreateTimer(animDuration), "timeout");
		// Create enemy attack collider
	}

	// Signaali joka saadaan kun health putoaa alle 0
	public async void OnDeath(float deathDelayTime) {
		isAlive = false;
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

	// Äänen toisto
	private void PlayAudioOnce(AudioStreamOggVorbis clip, int volume) {
		audioSource.Stream = clip;
		audioSource.VolumeDb = volume;
		audioSource.Play();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		pathFinder = GetNode<NavigationAgent3D>("Pathfinding");
		statHandler = GetNode<EnemyStats>("EnemyHandler");
		audioSource = GetNode<AudioStreamPlayer3D>("AudioPlayer");
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
		moveSpeed = statHandler.movementSpeed;
		pathFinder.PathDesiredDistance = 0.2f;
		pathFinder.TargetDesiredDistance = 0.2f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (isAlive) {
			Vector3 tempVelocity;
			playerDirection = (player.GlobalPosition - GlobalPosition).Normalized();
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (playerDistance < 5) {
				playerDetected = true;
			}
			// Detects player
			if (playerDetected == true) {
				LookAt(player.GlobalPosition);
				movementTarget = player.GlobalPosition;
				if (playerDistance < 0.5f && attackTimer >= 2) {
					SpiderAttack();
					attackTimer = 0;
					return;
				}
				else if (attackTimer < 2)
					attackTimer += delta;
			}
			// Moving around randomly or patrolling
			else {
				// Random Movement here
				if (pathFinder.IsNavigationFinished()) {
					return;
				}
			}
			MovementSetup();																	// Pathfinding Setup - etsi seuraava piste johon liikutaan
			Vector3 currentPosition = GlobalPosition;											// Otetaan oma positio
			Vector3 nextPathPosition = pathFinder.GetNextPathPosition();						// positio johon seuraavaksi siirrytään (pathfinding etsii pisteen)
			tempVelocity = currentPosition.DirectionTo(nextPathPosition) * moveSpeed * delta;	// tallennetaan suuntavectori velocitymuuttujaan
			if (!IsOnFloor())
				tempVelocity.Y -= gravity * delta;
			if (tempVelocity == Vector3.Zero) {
				// Idling animations
			}
			else {
				// Movement animations
			}
			// Liikkeen toteutus
			Velocity = tempVelocity;
			MoveAndSlide();
		}
	}
}
