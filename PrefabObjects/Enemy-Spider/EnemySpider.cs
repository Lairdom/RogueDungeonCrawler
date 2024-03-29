using Godot;
using System;
using System.Diagnostics;

public partial class EnemySpider : CharacterBody3D
{
	GameManager GM;
	Player player;
	Node3D root;
	Vector3 playerDirection;
	public EnemyStats statHandler;
	CollisionShape3D coll;
	CollisionShape3D attackCollider;
	AudioStreamPlayer3D audioSource;
	NavigationAgent3D pathFinder;
	Vector3 movementTarget;
	Vector3 targetPos;
	Rid navMap;
	float moveSpeed;
	bool seesPlayer, inSight;
	bool playerDetected = false;
	float attackTimer, skitterTimer, lerpTimer;
	float playerDistance, aggrRange;
	bool idling, attacking;
	private AnimationTree _animTree;
	float yPosTarget;
	float facing;
	Pathfinding nav;
	AudioStreamOggVorbis hitSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyHit2.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis deathSound = ResourceLoader.Load("res://Audio/SoundEffects/SpiderDeath1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis spiderAttack = ResourceLoader.Load("res://Audio/SoundEffects/SpiderAttack1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis spiderSkitter = ResourceLoader.Load("res://Audio/SoundEffects/SpiderWalk.ogg") as AudioStreamOggVorbis;
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
		await ToSignal(GetTree().CreateTimer(waitTime), "timeout");
		idling = false;
		int rng = 0;
		// Valitaan patrol piste riippuen missä huoneessa vihollinen spawnaa
		if (GM.currentRoom == 0)
			rng = GD.RandRange(1,5);
		else if (GM.currentRoom == 1)
			rng = GD.RandRange(6,10);
		else if (GM.currentRoom == 2)
			rng = GD.RandRange(11,18);												
		string path = "/root/World/PatrolPositions/PatrolPoint"+rng;				// Muutetaan path sen mukaisesti
		movementTarget = GetNodeOrNull<Node3D>(path).GlobalPosition;				// Etsitään kyseisen pisteen positio ja laitetaan se kohteeksi
		if (movementTarget == GlobalPosition)
			RandomPatrolPosition(0.01f);
		movementTarget.Y = 0.5f;
		if (!pathFinder.IsTargetReachable()) {
			Debug.Print("Target unreachable: "+this);
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
		if (body.Name == "Player" || body.Name == "ShieldCollider") {
			CalculateFacing();
			player.CalculateFacing();
			//Debug.Print("Difference: "+CalculateDifference());
			if (player.shieldIsUp && CalculateDifference() < 55)
				player.ShieldHit();
			else 
				player.PlayerTakeDamage(statHandler.damage);
		}
	}

	// Signaali joka saadaan kun pelaaja on vihollisen visioncolliderin sisällä
	private void OnVisionColliderEntered(Node3D body) {
		if (body.Name == "Player") {
			inSight = true;
		}
	}

	// Signaali joka saadaan kun pelaaja poistuu vihollisen visioncolliderin sisältä
	private void OnVisionColliderExited(Node3D body) {
		if (body.Name == "Player") {
			inSight = false;
		}
	}

	// Signaali joka saadaan kun health putoaa alle 0
	public async void OnDeath(float deathDelayTime) {
		statHandler.isAlive = false;
		GM.CheckAllEnemiesDefeated();
		coll.Disabled = true;
		attackCollider.Disabled = true;
		_animTree.Set("parameters/Death/blend_amount", 1.0);
		PlayAudioOnce(deathSound, -20);
		await ToSignal(GetTree().CreateTimer(deathDelayTime), "timeout");
		if (GM.araknoPhobiaMode == true)
			QueueFree();
	}

	// Signaali joka saadaan kun vihollinen ottaa damagea
	public void TakeDamage(int dmg) {
		playerDetected = true;
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

	//Funktio rotationin laskemiseksi
	private void CalculateFacing() {
		bool neg = false;
		facing = RotationDegrees.Y +270;
		if (facing < 0)
			neg = true;
		facing = Mathf.Abs(facing) % 360;
		if (neg)
			facing = 360-facing;
	}

	// Lasketaan erot 
	private float CalculateDifference() {
		float diff;
		diff = Mathf.Abs(facing-player.facing);
		if (diff > 180) {
			// Recalculate difference
			if (player.facing > facing)
				diff = facing+MathF.Abs(player.facing-360);
			else
				diff = player.facing+MathF.Abs(facing-360);
		}
		return diff;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		pathFinder = GetNode<NavigationAgent3D>("Pathfinding");
		statHandler = GetNode<EnemyStats>("EnemyHandler");
		attackCollider = GetNode<CollisionShape3D>("AttackCollider/Collider");
		coll = GetNode<CollisionShape3D>("Collider");
		audioSource = GetNode<AudioStreamPlayer3D>("AudioPlayer");
		_animTree = GetNode<AnimationTree>("AnimationTree");
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
		moveSpeed = statHandler.movementSpeed;
		aggrRange = statHandler.aggroRange;
		pathFinder.PathDesiredDistance = 0.5f;
		pathFinder.TargetDesiredDistance = 0.5f;
		yPosTarget = Scale.Y/2;
		nav = GetNodeOrNull<Pathfinding>("/root/World/PathingMap");
		if (Scale.Y == 0.5f)
			navMap = nav.smallMap;
		else if (Scale.Y == 1f)
			navMap = nav.smallMap;
		else if (Scale.Y == 1.5f)
			navMap = nav.normalMap;
		pathFinder.SetNavigationMap(navMap);
		RandomPatrolPosition(0);
		if (Scale.Y == 0.5f)
			statHandler.immunities.Add("Slashing");						// Jos pieni hämis niin huitaisut menevät yli
		if (GM.araknoPhobiaMode == true) {
			GetNode<Node3D>("Hamahakki").Hide();
			GetNode<MeshInstance3D>("OrbMesh").Show();
			GetNode<MeshInstance3D>("HeadMesh").Show();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (statHandler.isAlive) {
			Vector3 tempVelocity;
			playerDirection = (player.GlobalPosition - GlobalPosition).Normalized();
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (playerDistance < aggrRange && !playerDetected) {
				// Raycast pelaajaa kohti niin tiedetään onko näköyhteys pelaajaan
				var spaceState = GetWorld3D().DirectSpaceState;
				var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, player.GlobalPosition);
				var result = spaceState.IntersectRay(query);
				Node3D hitNode = (Node3D) result["collider"];
				seesPlayer = hitNode.Name == "Player" || hitNode.Name == "ShieldCollider";
				if (seesPlayer && inSight)
					playerDetected = true;
			}
			// Detects player
			if (playerDetected == true && GM.playerAlive) {
				targetPos = player.GlobalPosition;
				targetPos.Y = yPosTarget;
				LookAt(targetPos);
				movementTarget = targetPos;
				if (playerDistance < Scale.Y && attackTimer >= 2 && !attacking) {
					SpiderAttack();
					attackTimer = 0;
					return;
				}
				else if (attackTimer < 2)
					attackTimer += delta;
			}
			// Moving around randomly or patrolling
			else if (!idling) {
				targetPos = new Vector3(pathFinder.GetNextPathPosition().X, yPosTarget, pathFinder.GetNextPathPosition().Z);
				// Mikäli ei olla saavuttu valittuun pisteeseen, niin katsotaan kohti kyseistä pistettä
				if (!pathFinder.IsNavigationFinished() && GlobalPosition.X != targetPos.X && GlobalPosition.Z != targetPos.Z) {
					// Add Lerp to looking direction
					LookAt(targetPos);
				}
				// Jos ollaan saavuttu päätepisteeseen, haetaan uusi patrol piste
				else if (pathFinder.IsNavigationFinished()) {
					RandomPatrolPosition(2.5f);
				}
			}
			if (!attacking) {
				MovementSetup();																	// Pathfinding Setup - etsi seuraava piste johon liikutaan
				Vector3 currentPosition = GlobalPosition;											// Otetaan oma positio
				Vector3 nextPathPosition = pathFinder.GetNextPathPosition();						// positio johon seuraavaksi siirrytään (pathfinding etsii pisteen)
				tempVelocity = currentPosition.DirectionTo(nextPathPosition) * moveSpeed * delta;	// tallennetaan suuntavectori velocitymuuttujaan
				if (!IsOnFloor()) {
					AxisLockLinearY = false;
					tempVelocity.Y -= gravity * delta;
				}
				else	AxisLockLinearY = true;

				if (MathF.Abs(Velocity.Z) < 0.2 && MathF.Abs(Velocity.X) < 0.2) {
					// Idling animations
					_animTree.Set("parameters/Walk/blend_amount", 1.0);
				}
				else {
					// Movement animations
					_animTree.Set("parameters/Walk/blend_amount", 0.0);
					if (skitterTimer <= 0) {
						PlayAudioOnce(spiderSkitter, -10);
						skitterTimer = (float)GD.RandRange(0.1f, 0.3f);
					}
					else
						skitterTimer -= delta;
				}
				// Liikkeen toteutus
				Velocity = tempVelocity;
				MoveAndSlide();
			}
		}
	}
}
