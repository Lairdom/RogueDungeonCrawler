using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;


public partial class Player : CharacterBody3D
{
	[Export]public float moveSpeed = 100.0f;
	[Export]public float attackDuration = 3.0f;
	const float JUMPVELOCITY = 4.5f;
	Area3D playerRange = default;
	bool examine = false;
	Node3D target = default;
	int objectsInRange = 0;
	string[] stance = {"Slashing", "Piercing", "Bludgeoning"};
	int stanceIndex = 0;
	bool shieldIsUp = false;
	bool attacking = false;
	float attackTimer = 0f;
	Node3D ukkeli = default;
	AnimationPlayer anim = default;
		
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

	public override void _Ready() {
		//globalpath: GetNode<Area3D>("/root/World/Player/PlayerRange");
		playerRange = GetNode<Area3D>("PlayerRange");
		// AnimationPlayeriin viittaus onnistuu monella tapaa. Jos ne on tämän koodin lapsia niin ei tarvitse koko polkua
		ukkeli = GetNode<Node3D>("ukkeli");
		//anim = ukkeli.GetChild<AnimationPlayer>(1);  //toimii kans
		anim = GetNode<AnimationPlayer>("ukkeli/AnimationPlayer");
	}

	public override void _PhysicsProcess(double dDelta) {
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
			if (stanceIndex == 0) {
				Debug.Print("Slashing Attack!");
				anim.Play("MiekkaSlash");
				attackDuration = 2f;
			}
			else if (stanceIndex == 1) {
				Debug.Print("Poking Attack!");
				anim.Play("MiekkaStab");
				attackDuration = 1.5f;
			}
			else if (stanceIndex == 2) {
				Debug.Print("Smashing Attack!");
				anim.Play("MiekkaBash");
				attackDuration = 2f;
			}
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
			shieldIsUp = true;
		}
		else if (Input.IsActionJustReleased("Block") && !attacking) {
			Debug.Print("Shield is down");
			shieldIsUp = false;
		}
		

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
		if (direction != Vector3.Zero) {
			tempVelocity.X = direction.X * moveSpeed * delta;
			tempVelocity.Z = direction.Z * moveSpeed * delta;
		}
		else {
			tempVelocity.X = direction.X * 0;
			tempVelocity.Z = direction.Z * 0;
		}

		// Asetetaan tempVelocity muuttujan arvot uudeksi Velocityksi
		Velocity = tempVelocity;
		// MoveAndSlide on Godotin oma funktio joka hoitaa collisionit ja liikkeen
		MoveAndSlide();
	}
}
