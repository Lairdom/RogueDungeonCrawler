using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;


public partial class Player : CharacterBody3D
{
	[Signal] public delegate void StanceChangedEventHandler(string newStance);
	GameManager GM;
	public float moveSpeed = 75.0f;
	float attackDuration = 3.0f;
	const float JUMPVELOCITY = 4.5f;
	Area3D playerRange = default;
	StaticBody3D playerShield = default;
	Area3D attackCollider = default;
	CollisionShape3D atkCollShape = default;
	AudioStreamPlayer voiceAudioSource = default;
	AudioStreamPlayer sfxAudioSource = default;
	[ExportGroup ("Audio Clips")]
	[Export] AudioStreamOggVorbis playerHit;
	[Export] AudioStreamOggVorbis playerDeath, playerRaiseShield, footSteps, shieldHit, weaponSwing;						// insert clips via the inspector
	bool voicePlaying, sfxPlaying;
	bool examine = false;
	Node3D target = default;
	int objectsInRange = 0;
	string[] stance = {"Slashing", "Piercing", "Bludgeoning"};
	int stanceIndex = 0;
	Vector3[] atkCollPositions = {new Vector3(0.3f, 0.05f, 0), new Vector3(0.3f, 0.05f, 0), new Vector3(0.2f, 0.05f, 0)};
	Vector3[] atkCollSizes = {new Vector3(0.45f, 0.1f, 0.55f), new Vector3(0.7f, 0.1f, 0.1f), new Vector3(0.3f, 0.1f, 0.1f)};
	public bool shieldIsUp = false, raisingShield = false;
	public float facing;
	bool attacking = false;
	float attackTimer = 0f;
	float footStepTimer;
	Node3D ukkeli = default;
	private AnimationTree _animTree;
	string[] weaponType = {"Sword", "Spear", "Mace"};
	int curWeaponType = 0;					// currentWeaponType index. Indexiä vaihtamalla valitaan weaponType listasta asetyyppi
	MeshInstance3D weaponMesh = default;
	bool alive = true;
	public bool webbed = false;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	// Funktio jota kutsutaan kun pelaajan health laskee alle 0
	public void PlayerDeath() {
		alive = false;
		PlayAudioOnce(playerDeath, "Voice", -10);
		_animTree.Set("parameters/death/blend_amount", 1.0);
		Debug.Print("You died");
	}

	// Funktio jota kutsutaan vihollisten osuttua Playeriin
	public void PlayerTakeDamage(int damage) {
		GM.ChangePlayerHealth(-damage);
		if (GM.playerHealth > 0)
			PlayAudioOnce(playerHit, "Voice", -20);
	}

	// Funktio jota kutsutaan vihollisten osuttua pelaajan kilpeen
	public void ShieldHit() {
		PlayAudioOnce(shieldHit, "SFX", -10);
	}
	
	// Signaali joka saadaan kun objekti on pelaajan edessä
	private void OnPlayerRangeEntered(Node3D body) {
		if (body.HasMethod("OnActivate")) {
			objectsInRange++;
			target = body;
		}
	}

	// Signaali joka saadaan kun objekti poistuu pelaajan edestä
	private void OnPlayerRangeExited(Node3D body) {
		//Debug.Print("Object "+body.Name+" exited");
		if (body.HasMethod("OnActivate")) {
			objectsInRange--;
			target = null;
		}
	}

	// Signaali joka saadaan kun vihollinen on pelaajan attackColliderin sisällä sen kytkeytyessä päälle
	private void OnAttackColliderEntered(Node3D body) {
		if (body.HasMethod("TakeDamage")) {
			//Debug.Print("Enemy hit");
			body.CallDeferred("TakeDamage", GM.attackPower);
		}
	}

	private async void RaisingShield() {
		raisingShield = true;
		//float dur = _animTree.GetAnimation("kilpiBlock").Length;
		await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
		shieldIsUp = true;
		raisingShield = false;
	}

	// Vaihdetaan ase
	public void ChangeWeapon(int index) {
		// Viittaukset eri aseiden mesh
		MeshInstance3D sword = ukkeli.GetNode<MeshInstance3D>("Armature/Skeleton3D/Miekka");
		MeshInstance3D spear = ukkeli.GetNode<MeshInstance3D>("Armature/Skeleton3D/keihäs");
		MeshInstance3D mace = ukkeli.GetNode<MeshInstance3D>("Armature/Skeleton3D/Nuija");
		switch (index) {
			case 0:
				// Sword
				Debug.Print("Current Weapon is Sword");
				weaponMesh = sword;
				sword.Show();
				spear.Hide();
				mace.Hide();
				break;
			case 1:
				// Spear
				Debug.Print("Current Weapon is Spear");
				weaponMesh = spear;
				sword.Hide();
				spear.Show();
				mace.Hide();
				break;
			case 2:
				// Mace
				Debug.Print("Current Weapon is Mace");
				weaponMesh = mace;
				sword.Hide();
				spear.Hide();
				mace.Show();
				break;
		}
	}

	// Ääniefektien käynnistäminen
	private void PlayAudioOnce(AudioStreamOggVorbis clip, string type, int volume) {
		if (type == "Voice") {
			voiceAudioSource.Stream = clip;
			voiceAudioSource.VolumeDb = volume;
			voiceAudioSource.Play();
		}
		else if (type == "SFX" ) {
			sfxAudioSource.Stream = clip;
			sfxAudioSource.VolumeDb = volume;
			sfxAudioSource.Play();
		}
	}

	// Signaali joka saadaan kun ääni on viety loppuun
	private void OnVoiceAudioFinished() {
		voicePlaying = false;
	}

	// Signaali joka saadaan kun ääni on viety loppuun
	private void OnSFXAudioFinished() {
		sfxPlaying = false;
	}

	//Funktio rotationin laskemiseksi
	private void CalculateFacing() {
		bool neg = false;
		facing = RotationDegrees.Y;
		if (facing < 0)
			neg = true;
		facing = Mathf.Abs(facing) % 360;
		if (neg)
			facing = 360-facing;
	}

	private async void DestroyWebbing() {
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		// Audio effect?
		webbed = false;
	}

	// AttackCollider kytkeminen päälle ja pois
	private async void AttackColliderOnOff(float secs) {
		await ToSignal(GetTree().CreateTimer(secs), "timeout");			// vastaa Unityn yield WaitForSeconds()
		atkCollShape.Disabled = false;
		attackCollider.Show();
		await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
		atkCollShape.Disabled = true;
		attackCollider.Hide();
	}

	private void ChangeStance() {
		// Increase the stance index or loop back to the first stance
		stanceIndex = (stanceIndex + 1) % stance.Length;
		// Emit the StanceChanged signal with the new stance
		EmitSignal("StanceChanged", stance[stanceIndex]);
	}

	public override void _Ready() {
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		playerRange = GetNode<Area3D>("PlayerRange");
		ukkeli = GetNode<Node3D>("ukkeli");
		voiceAudioSource = GetNode<AudioStreamPlayer>("VoiceAudioPlayer");
		sfxAudioSource = GetNode<AudioStreamPlayer>("SFXAudioPlayer");
		playerShield = GetNode<StaticBody3D>("ShieldCollider");
		playerShield.Hide();
		attackCollider = GetNode<Area3D>("AttackCollider");
		atkCollShape = GetNode<CollisionShape3D>("AttackCollider/CollisionShape");
		atkCollShape.Disabled = true;
		attackCollider.Hide();
		_animTree = GetNode<AnimationTree>("AnimationTree");
		ChangeWeapon(curWeaponType);
		moveSpeed = GM.movementSpeed;
	}

	public override void _PhysicsProcess(double dDelta) {
		if (alive) {
			// Koska double tyylistä deltaa ei voi käyttää suoraan laskennassa, luodaan delta jonka avulla ei tarvitse aina castata deltaa floatiksi
			float delta = (float) dDelta;
			
			// Velocity on CharacterBody3D ominaisuus jota voidaan käyttää mutta sitä ei voi muokata komponentteina vaan se on aina Vektori
			// Siksi käytetään tilapäistä Vector3 muuttujaa koodissa ja asetetaan lopuksi se muuttuja Velocityn arvoksi
			Vector3 tempVelocity = Velocity;

			// Lisätään painovoima jos ei olla kosketuksissa maahan. Maa saadaan siitä missä kulmassa collaidataan
			if (!IsOnFloor())
				tempVelocity.Y -= gravity * delta;

			// Attack ('LeftClick')
			// Painettaessa nappia tehdään hyökkäys. Hyökkäyksen kesto riippuu aseesta ja hyökkäystyypistä
			if (Input.IsActionJustPressed("Attack") && IsOnFloor() && !shieldIsUp && !attacking) {
				attacking = true;
				attackCollider.Position = atkCollPositions[stanceIndex];
				atkCollShape.Scale = atkCollSizes[stanceIndex];
				float delay = 0;								// aika joka odotetaan ennen kuin laitetaan attackCollider päälle
				PlayAudioOnce(weaponSwing, "SFX", -20);
				if (stanceIndex == 0) {
					//Debug.Print("Slashing Attack!");
					attackDuration = 1f;
					_animTree.Set("parameters/Stance/blend_amount", 0.0);
					_animTree.Set("parameters/lyonti/request", 1);
					delay = 0.3f;
					DestroyWebbing();
				}
				else if (stanceIndex == 1) {
					//Debug.Print("Poking Attack!");
					attackDuration = 1f;
					_animTree.Set("parameters/Stance/blend_amount", -1.0);
					_animTree.Set("parameters/lyonti/request", 1);
					delay = 0.5f;
				}
				else if (stanceIndex == 2) {
					//Debug.Print("Smashing Attack!");
					attackDuration = 1f;
					_animTree.Set("parameters/Stance/blend_amount", 1.0);
					_animTree.Set("parameters/lyonti/request", 1);
					delay = 0.4f;
				}

				// Attack colliderin kytkeminen päälle ja pois 
				AttackColliderOnOff(delay);
			}

			// Odotetaan että edellinen hyökkäys on tehty
			if (attacking && attackTimer < attackDuration) {
				attackTimer+=delta;
			}
			else if (attacking && attackTimer >= attackDuration) {
				attacking = false;
				attackTimer = 0;
			}

			// Block ('RightClick')
			// Nappia pitämällä pohjassa kilpi on ylhäällä. Kilpeä ei voi nostaa ennen kuin hyökkäys on tehty loppuun.
			if (Input.IsActionPressed("Block") && IsOnFloor() && !attacking && !shieldIsUp && !raisingShield && !webbed) {
				_animTree.Set("parameters/KilpiBlock/blend_amount", 1.0);
				shieldIsUp = true;
			}
			else if (Input.IsActionJustReleased("Block") && !attacking || webbed) {
				_animTree.Set("parameters/KilpiBlock/blend_amount", 0.0);
				shieldIsUp = false;
			}
			
			if (shieldIsUp) {
				moveSpeed = GM.movementSpeed/3;
				_animTree.Set("parameters/kavelyNopeus/scale", 0.4);
				playerShield.CollisionLayer = 32;
				CalculateFacing();
			}
			else {
				moveSpeed = GM.movementSpeed;
				_animTree.Set("parameters/kavelyNopeus/scale", 1.0);
				playerShield.CollisionLayer = 0;
			}

			// StanceChange ('Q')
			// Nappia painamalla voidaan vaihtaa stancea. Napin painallus muuttaa indexiä. Käydään läpi stance niminen string array jossa eri stancen nimet.
			if (Input.IsActionJustPressed("StanceChange")) {
				ChangeStance();			
				//Debug.Print("Current Stance: "+stance[stanceIndex]);
			}

			// Jump (Space)
			// Input.IsActionPressed() jos nappia painaa tai se on pohjassa antaa true
			// Input.IsActionJustPressed() jos nappia painaa, pohjassa pitäminen ei tee mitään otetaan true vain kerran
			if (Input.IsActionJustPressed("Jump") && IsOnFloor() && !attacking && !webbed) {
				_animTree.Set("parameters/Jump/request", 1);
				tempVelocity.Y = JUMPVELOCITY;
			}

			// Interact ('E')
			if (Input.IsActionJustPressed("Examine") && objectsInRange > 0 && target != null && !shieldIsUp && !webbed) {
				Debug.Print("Called method: OnActivate");
				target.CallDeferred("OnActivate");
			}
			
			// Movement ('W'A'S'D')
			// Input.GetVector() ottaa 4 syöttöarvoa ja luo niistä vectorin. Käytetään Project Settings -> Input Map nimiä
			Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveBackwards", "MoveForwards");

			// Lukitaan hiiri ja piilotetaan se
			Input.MouseMode = Input.MouseModeEnum.Captured;
			
			// Muutetaan input suunta vektoriksi
			// Huomaa: 3D maailmassa Y-vektori on ylöspäin ja X ja Z vektorit luovat Y ja X suunnan. Kameran paikka ja pelaajan rotaatio vaikeuttavat asioita
			Vector3 direction = Transform.Basis * new Vector3(inputDir.Y, 0, inputDir.X);
			direction = direction.Normalized();		// Asetetaan vectorin suuruudeksi 1

			// Toteutetaan liike, jos direction on 0 niin pysäytetään pelaaja
			if (direction != Vector3.Zero && !webbed) {
				tempVelocity.X = direction.X * moveSpeed * delta;
				tempVelocity.Z = direction.Z * moveSpeed * delta;
				if (IsOnFloor()) {
					// Kävelyanimaatiot
					_animTree.Set("parameters/IdleWalk/blend_amount", 1.0);
					_animTree.Set("parameters/takaperin/blend_amount", 0.0);
					if (Input.IsActionPressed("MoveForwards")) {
						_animTree.Set("parameters/suunta/blend_amount", 0.0);
						_animTree.Set("parameters/takaperin/blend_amount", 0.0);
					}
					if (Input.IsActionPressed("MoveBackwards")) {
						_animTree.Set("parameters/takaperin/blend_amount", 1.0);
					}
					if (Input.IsActionPressed("MoveRight")) {
						_animTree.Set("parameters/suunta/blend_amount", 1.0);
						_animTree.Set("parameters/takaperin/blend_amount", 0.0);
					}
					if (Input.IsActionPressed("MoveLeft")) {
						_animTree.Set("parameters/suunta/blend_amount", -1.0);
					}
					if (footStepTimer <= 0) {
						PlayAudioOnce(footSteps, "SFX", -20);
						if (shieldIsUp) {footStepTimer = 1.2f;}
						else {footStepTimer = 0.5f;}
					}
					else {	footStepTimer -= delta;		}
				}
			}
			else {
				tempVelocity.X = direction.X * 0;
				tempVelocity.Z = direction.Z * 0;
				if (IsOnFloor())				
					_animTree.Set("parameters/IdleWalk/blend_amount", 0.0);
				if (webbed)
					_animTree.Set("parameters/kavelyNopeus/scale", 0);
			}
			Velocity = tempVelocity;		// Asetetaan tempVelocity muuttujan arvot uudeksi Velocityksi
			MoveAndSlide();					// MoveAndSlide on Godotin oma funktio joka hoitaa collisionit ja liikkeen
		}
		else {
			// Player is dead
			Vector3 tempVelocity = Velocity;
			tempVelocity.X = 0;
			tempVelocity.Z = 0;
			Velocity = tempVelocity;
			MoveAndSlide();
		}
	}
}
