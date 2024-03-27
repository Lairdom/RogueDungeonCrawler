using Godot;
using System;
using System.Diagnostics;

public partial class EnemySkeleton : CharacterBody3D
{
	GameManager GM;
	Player player;
	Node3D root;
	Vector3 playerDirection;
	public EnemyStats statHandler;
	CollisionShape3D coll;
	CollisionShape3D attackCollider;
	AudioStreamPlayer3D audioSource;
	float playerDistance;
	NavigationAgent3D pathFinder;
	Pathfinding nav;
	Rid navMap;
	Vector3 movementTarget;
	Vector3 targetPos;
	float moveSpeed;
	bool seesPlayer, inSight, playerDetected = false;
	float attackTimer, footStepsTimer;
	float aggrRange;
	bool idling, attacking;
	float yPosTarget;
	float facing;
	AudioStreamOggVorbis hitSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyHit1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis deathSound = ResourceLoader.Load("res://Audio/SoundEffects/PlayerWork.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis skeletonAttack = ResourceLoader.Load("res://Audio/SoundEffects/WeaponSwing.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis footSteps = ResourceLoader.Load("res://Audio/SoundEffects/RockyFootsteps.ogg") as AudioStreamOggVorbis;
	private AnimationTree _animTree;
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
			RandomPatrolPosition(0);
		movementTarget.Y = 0.5f;
		if (!pathFinder.IsTargetReachable()) {
			Debug.Print("Target unreachable");
			RandomPatrolPosition(0);
		}
	}

	// Signaali joka saadaan kun health putoaa alle 0
	public async void OnDeath(float deathDelayTime) {
		statHandler.isAlive = false;
		GM.CheckAllEnemiesDefeated();
		coll.Disabled = true;
		attackCollider.Disabled = true;
		// Kuolema animaatio
		_animTree.Set("parameters/die/blend_amount", 1.0);
		PlayAudioOnce(deathSound, -20);
		await ToSignal(GetTree().CreateTimer(deathDelayTime), "timeout");
		
	}

	// Signaali joka saadaan kun vihollinen ottaa damagea
	public void TakeDamage(int dmg) {
		playerDetected = true;
		statHandler.ChangeHealth(dmg);
		if (statHandler.currentHealth > 0) {
			PlayAudioOnce(hitSound, -20);
		}
	}

	// Luurangon melee hyökkäys
	private async void SkeletonMeleeAttack() {
		attacking = true;
		// Luurangon hyökkäysanimaatiot
		
		float animDuration = 1;
		PlayAudioOnce(skeletonAttack, -20);
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

	// Äänen toisto
	private void PlayAudioOnce(AudioStreamOggVorbis clip, int volume) {
		audioSource.Stream = clip;
		audioSource.VolumeDb = volume;
		audioSource.Play();
	}

	//Funktio oman katselusuunnan laskemiseksi (asteissa)
	private void CalculateFacing() {
		bool neg = false;
		facing = RotationDegrees.Y +270;
		if (facing < 0)
			neg = true;
		facing = Mathf.Abs(facing) % 360;
		if (neg)
			facing = 360-facing;
	}

	// Lasketaan erot pelaajan ja vihun katse suunnissa  (tarvitaan jotta tiedetään mihin suuntaan pelaajan kilpi osoittaa)
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

	public override void _Ready() {
		// Omat muuttujat
		pathFinder = GetNode<NavigationAgent3D>("Pathfinding");
		statHandler = GetNode<EnemyStats>("EnemyHandler");
		coll = GetNode<CollisionShape3D>("Collider");
		audioSource = GetNode<AudioStreamPlayer3D>("AudioPlayer");
		attackCollider = GetNode<CollisionShape3D>("AttackCollider/Collider");
		// Ulkoiset muuttujat
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		_animTree = GetNode<AnimationTree>("AnimationTree");
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
		// Pathfinding alustuksia
		moveSpeed = statHandler.movementSpeed;
		aggrRange = statHandler.aggroRange;
		pathFinder.PathDesiredDistance = 0.5f;
		pathFinder.TargetDesiredDistance = 0.5f;
		yPosTarget = 0.25f;
		nav = GetNodeOrNull<Pathfinding>("/root/World/PathingMap");
		navMap = nav.smallMap;
		pathFinder.SetNavigationMap(navMap);
		RandomPatrolPosition(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (statHandler.isAlive) {
			Vector3 tempVelocity;
			playerDirection = (player.GlobalPosition - GlobalPosition).Normalized();
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (playerDistance < aggrRange) {
				// Raycast pelaajaa kohti jotta tiedetään onko bossilla näköyhteys pelaajaan
				var spaceState = GetWorld3D().DirectSpaceState;
				var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, player.GlobalPosition);
				var result = spaceState.IntersectRay(query);
				Node3D hitNode = (Node3D) result["collider"];
				seesPlayer = hitNode.Name == "Player" || hitNode.Name == "ShieldCollider";
				if (seesPlayer && inSight)
					playerDetected = true;
			}
			// Pelaaja havaitaan
			if (playerDetected == true && GM.playerAlive) {
				targetPos = player.GlobalPosition;
				targetPos.Y = yPosTarget;
				LookAt(targetPos);
				movementTarget = targetPos;
								
				// Kun ollaan tarpeeksi lähellä tehdään melee hyökkäys
				if (playerDistance <= 0.5 && attackTimer >= 2 && !attacking) {
					SkeletonMeleeAttack();
					_animTree.Set("parameters/Lyo/request", 1);
					attackTimer = 0;
					return;
				}
				else if (attackTimer < 2)
					attackTimer += delta;
			}
			// Kävelee satunnaisesti tai kulkee sovittua reittiä
			else if (!idling) {
				targetPos = new Vector3(pathFinder.GetNextPathPosition().X, yPosTarget, pathFinder.GetNextPathPosition().Z);
				// Mikäli ei olla saavuttu valittuun pisteeseen, niin katsotaan kohti kyseistä pistettä
				if (!pathFinder.IsNavigationFinished() && GlobalPosition.X != targetPos.X && GlobalPosition.Z != targetPos.Z) {
					// Add Lerp to looking direction
					LookAt(targetPos);
				}
				// Jos ollaan saavuttu päätepisteeseen, haetaan uusi patrol piste
				else if (pathFinder.IsNavigationFinished() && !idling) {
					RandomPatrolPosition(2.5f);
				}
			}
			if (!attacking && playerDistance > 0.5f) {
				MovementSetup();																	// Pathfinding Setup - etsi seuraava piste johon liikutaan
				Vector3 currentPosition = GlobalPosition;											// Otetaan oma positio
				Vector3 nextPathPosition = pathFinder.GetNextPathPosition();						// positio johon seuraavaksi siirrytään (pathfinding etsii pisteen)
				tempVelocity = currentPosition.DirectionTo(nextPathPosition) * moveSpeed * delta;	// tallennetaan suuntavectori velocitymuuttujaan
				if (!IsOnFloor()) {
					AxisLockLinearY = false;
					tempVelocity.Y -= gravity * delta;
				}
				else
					AxisLockLinearY = true;
				if (MathF.Abs(Velocity.Z) < 0.2 && MathF.Abs(Velocity.X) < 0.2) {
					// Idling animations
					_animTree.Set("parameters/Kavely/blend_amount", 0.0);
	
				}
				else {
					// Movement animations
					_animTree.Set("parameters/Kavely/blend_amount", 1.0);
					
					if (footStepsTimer <= 0) {
						PlayAudioOnce(footSteps, -20);
						footStepsTimer = (float)GD.RandRange(0.1f, 0.3f);
					}
					else
						footStepsTimer -= delta;
				}
				// Liikkeen toteutus
				Velocity = tempVelocity;
				MoveAndSlide();
			}
		}
	}
}
