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
	CollisionShape3D attackCollider;
	AudioStreamPlayer3D audioSource;
	bool isAlive = true;
	float playerDistance;
	NavigationAgent3D pathFinder;
	Vector3 movementTarget;
	float moveSpeed;
	bool playerDetected = false;
	float attackTimer;
	float lerpTimer;
	float aggrRange;
	bool idling;
	private AnimationTree _animTree;
	bool attacking;
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

	// Random patrol position
	private async void RandomPatrolPosition(float waitTime) {
		idling = true;
		lerpTimer = 0;
		await ToSignal(GetTree().CreateTimer(waitTime), "timeout");
		idling = false;
		int rng = GD.RandRange(1,5);
		string path = "/root/World/PatrolPositions/PatrolPoint"+rng;
		Vector3 randomPosition = GetNodeOrNull<Node3D>(path).GlobalPosition;
		Debug.Print(""+pathFinder.GetNextPathPosition());
		movementTarget = randomPosition;
		if (!pathFinder.IsTargetReachable()) {
			Debug.Print("Target unreachable");
		}
	}

	// Hämähäkin melee hyökkäys
	private async void SpiderAttack() {
		attacking = true;
		// SpiderAttack animations
		_animTree.Set("parameters/OneShot/request", 1);
		float animDuration = 1;
		PlayAudioOnce(spiderAttack, -20);
		await ToSignal(GetTree().CreateTimer(animDuration/2), "timeout");
		attackCollider.Disabled = false;
		await ToSignal(GetTree().CreateTimer(animDuration/4), "timeout");
		attackCollider.Disabled = true;
		attacking = false;
	}

	// Signaali joka saadaan kun joko pelaaja tai shieldcollider on vihollisen hyökkäyscolliderin sisällä
	private void OnAttackColliderEntered(Node3D body) {
		Debug.Print("Collider entered: "+body.Name);
		if (body.Name == "Player") {
			player.PlayerTakeDamage(statHandler.damage);
		}
		else if (body.Name == "ShieldCollider") {
			player.ShieldHit();
		}
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
		attackCollider = GetNode<CollisionShape3D>("AttackCollider/Collider");
		audioSource = GetNode<AudioStreamPlayer3D>("AudioPlayer");
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		root = GetNodeOrNull<Node3D>("/root/World");
		_animTree = GetNode<AnimationTree>("AnimationTree");
		player = GetNodeOrNull<Player>("/root/World/Player");
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
		moveSpeed = statHandler.movementSpeed;
		aggrRange = statHandler.aggroRange;
		pathFinder.PathDesiredDistance = 0.5f;
		pathFinder.TargetDesiredDistance = 0.5f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (isAlive) {
			Vector3 tempVelocity;
			playerDirection = (player.GlobalPosition - GlobalPosition).Normalized();
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (playerDistance < aggrRange) {
				playerDetected = true;
			}
			// Detects player
			if (playerDetected == true) {
				LookAt(player.GlobalPosition);
				movementTarget = player.GlobalPosition;
				if (playerDistance < 0.75f && attackTimer >= 2 && !attacking) {
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
				if (pathFinder.IsNavigationFinished() && !idling) {
					RandomPatrolPosition(2.5f);
				}
				else if (Position.DistanceTo(pathFinder.GetNextPathPosition()) > 0.5f) {
					LookAt(pathFinder.GetNextPathPosition());
					// Lerp turning towards target
					if (lerpTimer < 1)
						lerpTimer += delta;
				}
			}
			if (!attacking) {
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
}
