using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class EnemyBoss : CharacterBody3D
{
	GameManager GM;
	Player player;
	Node3D root, groundNode;
	Vector3 playerDirection;
	public EnemyStats statHandler;
	CollisionShape3D coll;
	CollisionShape3D attackCollider;
	AudioStreamPlayer3D audioSource, moveSFXSource;
	float playerDistance;
	Pathfinding nav;
	Rid navMap;
	NavigationAgent3D pathFinder;
	Vector3 movementTarget;
	Vector3 targetPos;
	float moveSpeed;
	bool playerDetected = false;
	float attackTimer, skitterTimer, wingFlapTimer, flightTimer, groundedTimer;
	private AnimationTree _animTree;
	bool flying, idling, attacking, landing, takingFlight;
	bool seesPlayer;
	public bool caughtPlayer;
	float yPosTarget;
	float facing;
	int smacks;
	AudioStreamOggVorbis hitSound = ResourceLoader.Load("res://Audio/SoundEffects/EnemyHit2.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis deathSound = ResourceLoader.Load("res://Audio/SoundEffects/SpiderDeath1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis spiderAttack = ResourceLoader.Load("res://Audio/SoundEffects/SpiderAttack1.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis spiderSkitter = ResourceLoader.Load("res://Audio/SoundEffects/SpiderWalk.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis wingsFlapping = ResourceLoader.Load("res://Audio/SoundEffects/InsectWings.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis webFlying = ResourceLoader.Load("res://Audio/SoundEffects/WeaponSwing.ogg") as AudioStreamOggVorbis;
	PackedScene webAttackScene = ResourceLoader.Load("res://PrefabObjects/Enemy-Boss/WebAttack.tscn") as PackedScene;
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

	// Signaali joka saadaan kun health putoaa alle 0
	public async void OnDeath(float deathDelayTime) {
		statHandler.isAlive = false;
		Debug.Print("Victory!!!");
		coll.Disabled = true;
		attackCollider.Disabled = true;
		// Kuolema animaatio
		_animTree.Set("parameters/die/blend_amount", 1.0);

		PlayAudioOnce(deathSound, 0, -20);
		await ToSignal(GetTree().CreateTimer(deathDelayTime), "timeout");
		
	}

	// Signaali joka saadaan kun vihollinen ottaa damagea
	public void TakeDamage(int dmg) {
		smacks++;
		statHandler.ChangeHealth(dmg);
		if (statHandler.currentHealth > 0) {
			PlayAudioOnce(hitSound, 0, -20);
		}
		if (groundedTimer > 10 || smacks%3 == 0) 
			BossTakeFlight();
	}

	// Bossin melee hyökkäys
	private async void BossMeleeAttack() {
		attacking = true;
		// Melee hyökkäys animaatio
		_animTree.Set("parameters/hVaihto/blend_amount", 0.0);
		_animTree.Set("parameters/OneShot/request", 1);
		
		Debug.Print("Melee Attack");
		float animDuration = 1;
		PlayAudioOnce(spiderAttack, 0, -20);
		await ToSignal(GetTree().CreateTimer(animDuration/2), "timeout");
		attackCollider.Disabled = false;
		await ToSignal(GetTree().CreateTimer(animDuration/4), "timeout");
		attackCollider.Disabled = true;
		attacking = false;
	}

	// Bossin seitti hyökkäys
	private async void BossWebAttack() {
		attacking = true;
		// Seitti hyökkäys animaatio
		_animTree.Set("parameters/hVaihto/blend_amount", 1.0);
		_animTree.Set("parameters/OneShot/request", 1);

		SpawnWebShot();
		float animDuration = 2;
		PlayAudioOnce(spiderAttack, 0, -20);
		await ToSignal(GetTree().CreateTimer(animDuration), "timeout");
		// Instantiate web
		attacking = false;
		// Boss lands after a set duration if it has not landed yet
		if (flightTimer > 30 || caughtPlayer == true)
			BossLanding();
	}

	private void SpawnWebShot() {
		WebAttack webInstance = (WebAttack) webAttackScene.Instantiate();
		webInstance.boss = this;
		webInstance.Position = Position;
		webInstance.Rotation = Rotation;
		root.AddChild(webInstance);
		PlayAudioOnce(webFlying, 0, -20);
	}

	// Boss laskeutuu maahan
	private async void BossLanding() {
		landing = true;
		flying = false;
		Debug.Print("Landing");
		groundedTimer = 0;
		// laskeutumis animaatio?
		_animTree.Set("parameters/nousu/blend_amount", 1.0);
		_animTree.Set("parameters/OneShot2/request", 1);
		await ToSignal(GetTree().CreateTimer(2), "timeout");
		landing = false;
	}

	// Boss lähtee uudestaan lentoon
	private async void BossTakeFlight() {
		takingFlight = true;
		Debug.Print("Taking flight");
		flying = true;
		flightTimer = 0;
		// lentoon lähtö animaatio?
		_animTree.Set("parameters/nousu/blend_amount", 0.0);
		_animTree.Set("parameters/OneShot2/request", 1);
		await ToSignal(GetTree().CreateTimer(2), "timeout");
		takingFlight = false;
		caughtPlayer = false;
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

	// Äänen toisto
	private void PlayAudioOnce(AudioStreamOggVorbis clip, int source, int volume) {
		if (source == 0) {
			audioSource.Stream = clip;
			audioSource.VolumeDb = volume;
			audioSource.Play();
		}
		else {
			moveSFXSource.Stream = clip;
			moveSFXSource.VolumeDb = volume;
			moveSFXSource.Play();
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Omat muuttujat
		pathFinder = GetNode<NavigationAgent3D>("Pathfinding");
		statHandler = GetNode<EnemyStats>("EnemyHandler");
		coll = GetNode<CollisionShape3D>("Collider");
		audioSource = GetNode<AudioStreamPlayer3D>("AudioPlayer");
		moveSFXSource = GetNode<AudioStreamPlayer3D>("MoveSFXAudioPlayer");
		groundNode = GetNode<Node3D>("GroundPathingNode");
		attackCollider = GetNode<CollisionShape3D>("AttackCollider/Collider");
		// Ulkoiset muuttujat
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		_animTree = GetNode<AnimationTree>("AnimationTree");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		nav = GetNodeOrNull<Pathfinding>("/root/World/PathingMap");
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
		// Pathfinding alustuksia
		flying = true;
		moveSpeed = statHandler.movementSpeed;
		pathFinder.PathDesiredDistance = 0.5f;
		pathFinder.TargetDesiredDistance = 0.5f;
		yPosTarget = 1.5f;
		groundNode.GlobalPosition = new Vector3(GlobalPosition.X, 0.5f, GlobalPosition.Z);
		navMap = nav.largeMap;
		pathFinder.SetNavigationMap(navMap);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (statHandler.isAlive) {
			Vector3 tempVelocity = Vector3.Zero;
			playerDirection = Position.DirectionTo(player.Position);
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			// Koska boss, niin pelaaja havaitaan automaattisesti
			if (GM.playerAlive) {
				targetPos = player.GlobalPosition;
				LookAt(player.GlobalPosition);

				// Raycast pelaajaa kohti jotta tiedetään onko bossilla näköyhteys pelaajaan
				var spaceState = GetWorld3D().DirectSpaceState;
				var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, player.GlobalPosition);
				var result = spaceState.IntersectRay(query);
				Node3D hitNode = (Node3D) result["collider"];
				seesPlayer = hitNode.Name == "Player" || hitNode.Name == "ShieldCollider";
				
				// Jos boss on ilmassa mutta liian kaukana tai ei näe pelaajaa
				if (flying && playerDistance > 6 && !attacking && !landing && !takingFlight || !seesPlayer) {
					targetPos.Y = yPosTarget;
					movementTarget = targetPos;
				}
				// Jos boss on ilmassa ja hyvällä etäisyydellä seitti hyökkäykseen
				else if (flying && playerDistance <= 6 && seesPlayer && attackTimer >= 2 && !attacking && !landing && !takingFlight) {
					BossWebAttack();
					attackTimer = 0;
					return;
				}
				// Jos boss on laskeutunut ja lähestyy pelaajaa
				else if (!flying && playerDistance > 1.6f && !attacking && !landing) {
					targetPos.Y = 0.25f;
					movementTarget = targetPos;
				}
				// Jos boss on laskeutunut ja melee etäisyydellä
				else if (!flying && playerDistance <= 1.6f && attackTimer >= 2 && !attacking && !landing) {
					BossMeleeAttack();
					attackTimer = 0;
					return;
				}
				if (attackTimer < 2) {	attackTimer += delta;	}
			}
			// Liikkuminen
			if (!attacking && !landing && !takingFlight) {
				groundNode.GlobalPosition = new Vector3(groundNode.GlobalPosition.X, 0.5f, groundNode.GlobalPosition.Z);
				Vector3 currentPosition;
				MovementSetup();																	// Pathfinding Setup - etsi seuraava piste johon liikutaan
				if (flying) {	currentPosition = groundNode.GlobalPosition;	}					// Otetaan oma positio groundNoden positiosta									
				else {	currentPosition = GlobalPosition;	}										// Otetaan oma positio itsestä							
				Vector3 nextPathPosition = pathFinder.GetNextPathPosition();						// positio johon seuraavaksi siirrytään (pathfinding etsii pisteen)
				tempVelocity = currentPosition.DirectionTo(nextPathPosition) * moveSpeed * delta;	// tallennetaan suuntavectori velocitymuuttujaan
				
				if (flying && MathF.Abs(Velocity.Z) < 0.2 && MathF.Abs(Velocity.X) < 0.2) {
					// Idling animations in the air
					
				}
				else if (!flying && MathF.Abs(Velocity.Z) < 0.2 && MathF.Abs(Velocity.X) < 0.2) {
					// Idling animations on the ground
					_animTree.Set("parameters/idle/blend_amount", 1.0);
					
				}
				else if (flying) {
					// Movement animations in the air
					_animTree.Set("parameters/liike/blend_amount", 1.0);
					_animTree.Set("parameters/idle/blend_amount", 0.0);

					
				}
				else {
					// Movement animations on the ground
					_animTree.Set("parameters/liike/blend_amount", 0.0);
					_animTree.Set("parameters/idle/blend_amount", 0.0);
					
					
					if (skitterTimer <= 0) {
						PlayAudioOnce(spiderSkitter, 1, -10);
						skitterTimer = (float)GD.RandRange(0.1f, 0.3f);
					}
					else {	skitterTimer -= delta;	}
				}
			}
			// Liike kun noustaan takaisin lentoon
			else if (takingFlight) {
				targetPos.Y = yPosTarget;
				Vector3 takeFlightDirection = targetPos;
				takeFlightDirection.Y = -3f;
				tempVelocity = groundNode.GlobalPosition.DirectionTo(takeFlightDirection) * moveSpeed * -2 * delta;
			}
			// Kun ei lennetä niin painovoima vaikuttaa. Nostetaan maassaolo aikaa sekä lentoaikaa niiden ollessa päällä.
			if (!flying) {
				groundedTimer += delta;	
				tempVelocity.Y -= gravity * delta * 10;
			}
			else {	
				flightTimer += delta;
				if (wingFlapTimer <= 0) {
					PlayAudioOnce(wingsFlapping, 1, -10);
					wingFlapTimer = 0.1f;
				}
				else {	wingFlapTimer -= delta;	}
			}

			// Liikkeen toteutus
			Velocity = tempVelocity;
			MoveAndSlide();
		}
	}
}
