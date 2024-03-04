using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;


public partial class Player : CharacterBody3D
{
	[Export]public float moveSpeed = 75.0f;
	[Export]public float attackDuration = 3.0f;
	const float JUMPVELOCITY = 4.5f;
	Area3D playerRange = default;
	StaticBody3D playerShield = default;
	Area3D attackCollider = default;
	CollisionShape3D atkCollShape = default;
	MeshInstance3D tempShape = default;					//remove
	bool examine = false;
	Node3D target = default;
	int objectsInRange = 0;
	string[] stance = {"Slashing", "Piercing", "Bludgeoning"};
	int stanceIndex = 0;
	Vector3[] atkCollPositions = {new Vector3(0.3f, 0.05f, 0), new Vector3(0.3f, 0.05f, 0), new Vector3(0.2f, 0.05f, 0)};
	Vector3[] atkCollSizes = {new Vector3(0.45f, 0.1f, 0.55f), new Vector3(0.7f, 0.1f, 0.1f), new Vector3(0.3f, 0.1f, 0.1f)};
	bool shieldIsUp = false;
	bool attacking = false;
	float attackTimer = 0f;
	Node3D ukkeli = default;
	private AnimationPlayer _animPlayer;
	string[] weaponType = {"Sword", "Spear", "Mace"};
	int curWeaponType = 0;					// currentWeaponType index. Indexiä vaihtamalla valitaan weaponType listasta asetyyppi
	MeshInstance3D weaponMesh = default;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	// Signaali joka saadaan kun objekti on pelaajan edessä
	private void OnPlayerRangeEntered(Node3D body) {
		//Debug.Print("Object "+body.Name+" entered.");
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

	// Signaali joka saadaan kun vihollinen on pelaajan attackColliderin sisällä
	private void OnAttackColliderEntered(Node3D body) {
		if (body.HasMethod("TakeDamage")) {
			Debug.Print("Enemy hit");
			body.CallDeferred("TakeDamage", 32);
		}
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

	private async void Wait(double ms) {
		await ToSignal(GetTree().CreateTimer(ms), "timeout");
		
	}

	public override void _Ready() {
		//globalpath: GetNode<Area3D>("/root/World/Player/PlayerRange");
		playerRange = GetNode<Area3D>("PlayerRange");
		ukkeli = GetNode<Node3D>("ukkeli");
		playerShield = GetNode<StaticBody3D>("ShieldCollider");
		playerShield.Hide();
		attackCollider = GetNode<Area3D>("AttackCollider");
		atkCollShape = GetNode<CollisionShape3D>("AttackCollider/CollisionShape");
		tempShape = GetNode<MeshInstance3D>("AttackCollider/TempMesh");
		atkCollShape.Disabled = true;
		attackCollider.Hide();
		//Animaatiokoodi: yritän löytää ukkelin animationPlayerin
		_animPlayer = GetNode<AnimationPlayer>("ukkeli/AnimationPlayer");
		ChangeWeapon(curWeaponType);
	}

	public override async void _PhysicsProcess(double dDelta) {
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
			tempShape.Scale = atkCollSizes[stanceIndex];
			float delay = 0;								// aika joka odotetaan ennen kuin laitetaan attackCollider päälle
			if (stanceIndex == 0) {
				//Debug.Print("Slashing Attack!");
				attackDuration = 1f;
				_animPlayer.Play("miekkaSlash");
				delay = 0.3f;
			}
			else if (stanceIndex == 1) {
				//Debug.Print("Poking Attack!");
				attackDuration = 1f;
				_animPlayer.Play("miekkaStab");
				delay = 0.5f;
			}
			else if (stanceIndex == 2) {
				//Debug.Print("Smashing Attack!");
				attackDuration = 1f;
				_animPlayer.Play("miekkaBash");
				delay = 0.4f;
			}
			
			await ToSignal(GetTree().CreateTimer(delay), "timeout");			// vastaa Unityn yield WaitForSeconds()
			atkCollShape.Disabled = false;
			attackCollider.Show();
			await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
			atkCollShape.Disabled = true;
			attackCollider.Hide();
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
		if (Input.IsActionPressed("Block") && IsOnFloor() && !attacking && !shieldIsUp) {
			Debug.Print("Shield is up");
			_animPlayer.Play("kilpiBlock");
			shieldIsUp = true;
		}
		else if (Input.IsActionJustReleased("Block") && !attacking) {
			Debug.Print("Shield is down");
			_animPlayer.Play("kilpiDown");
			shieldIsUp = false;
		}
		
		if (shieldIsUp)
			playerShield.CollisionLayer = 32;
		else
			playerShield.CollisionLayer = 0;

		// StanceChange ('Q')
		// Nappia painamalla voidaan vaihtaa stancea. Napin painallus muuttaa indexiä. Käydään läpi stance niminen string array jossa eri stancen nimet.
		if (Input.IsActionJustPressed("StanceChange")) {
			Debug.Print("Changed Stance");
			if (stanceIndex < 2)
				stanceIndex++;
			else
				stanceIndex = 0;
			
			Debug.Print("Current Stance: "+stance[stanceIndex]);
		}

		// Jump (Space)
		// Input.IsActionPressed() jos nappia painaa tai se on pohjassa antaa true
		// Input.IsActionJustPressed() jos nappia painaa, pohjassa pitäminen ei tee mitään otetaan true vain kerran
		if (Input.IsActionJustPressed("Jump") && IsOnFloor() && !attacking)
			tempVelocity.Y = JUMPVELOCITY;

		// Interact ('E')
		if (Input.IsActionJustPressed("Examine") && objectsInRange > 0 && target != null && !shieldIsUp) {
			Debug.Print("Called method: OnActivate");
			target.CallDeferred("OnActivate");
		}
		
		// Movement ('W'A'S'D')
		// Input.GetVector() ottaa 4 syöttöarvoa ja luo niistä vectorin. Käytetään Project Settings -> Input Map nimiä
		Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveBackwards", "MoveForwards");

		// Lukitaan hiiri ja piilotetaan se
		//Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Muutetaan input suunta vektoriksi
		// Huomaa: 3D maailmassa Y-vektori on ylöspäin ja X ja Z vektorit luovat Y ja X suunnan. Kameran paikka ja pelaajan rotaatio vaikeuttavat asioita
		Vector3 direction = Transform.Basis * new Vector3(inputDir.Y, 0, inputDir.X);
		direction = direction.Normalized();		// Asetetaan vectorin suuruudeksi 1

		// Toteutetaan liike, jos direction on 0 niin pysäytetään pelaaja
		if (direction != Vector3.Zero && !attacking) {
			tempVelocity.X = direction.X * moveSpeed * delta;
			tempVelocity.Z = direction.Z * moveSpeed * delta;
			_animPlayer.Play("walkMiekkaKilpi");
		}
		else {
			tempVelocity.X = direction.X * 0;
			tempVelocity.Z = direction.Z * 0;
			if (!attacking && !shieldIsUp)				
				_animPlayer.Play("Idle");
		}

		// Asetetaan tempVelocity muuttujan arvot uudeksi Velocityksi
		Velocity = tempVelocity;
		// MoveAndSlide on Godotin oma funktio joka hoitaa collisionit ja liikkeen
		MoveAndSlide();
	}
}
