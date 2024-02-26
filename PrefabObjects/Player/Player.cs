using Godot;
using System;
using System.Diagnostics;

public partial class Player : CharacterBody3D
{
	public const float JUMPVELOCITY = 4.5f;
	public float moveSpeed = 100.0f;
	Area3D playerRange = default;
	bool inRange = false;
	
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	private void OnPlayerRangeEntered(Node3D body) {
		Debug.Print("Object "+body.Name+" entered");
		
	}

	private void OnPlayerRangeExited(Node3D body) {
		Debug.Print("Object "+body.Name+" exited");
		
	}

	public override void _Ready() {
		playerRange = GetNode<Area3D>("PlayerRange");
	}

	public override void _PhysicsProcess(double delta) {
		// Koska double tyylistä delta ei voi käyttää suoraan laskennassa, luodaan deltaF jonka avulla ei tarvitse aina castata deltaa floatiksi
		float deltaF = (float) delta;
		
		// Velocity on CharacterBody3D ominaisuus jota voidaan käyttää mutta sitä ei voi muokata komponentteina vaan se on aina Vektori
		// Siksi käytetään tilapäistä Vector3 muuttujaa koodissa ja asetetaan lopuksi se muuttuja Velocityn arvoksi
		Vector3 tempVelocity = Velocity;

		// Lisätään painovoima jos ei olla kosketuksissa maahan. Maa saadaan siitä missä kulmassa collaidataan
		if (!IsOnFloor())
			tempVelocity.Y -= gravity * deltaF;

		// Jump
		// Input.IsActionPressed() jos nappia painaa tai se on pohjassa antaa true
		// Input.IsActionJustPressed() jos nappia painaa, pohjassa pitäminen ei tee mitään otetaan true vain kerran
		if (Input.IsActionJustPressed("Jump") && IsOnFloor())
			tempVelocity.Y = JUMPVELOCITY;

		// Otetaan input
		// Input.GetVector() ottaa 4 syöttöarvoa ja luo niistä vectorin. Käytetään Project Settings -> Input Map nimiä
		Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveBackwards", "MoveForwards");

		// Lukitaan hiiri ja piilotetaan se
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Muutetaan input suunta vektoriksi
		// Huomaa: 3D maailmassa Y-vektori on ylöspäin ja X ja Z vektorit luovat Y ja X suunnan. Kameran paikka ja pelaajan rotaatio vaikeuttavat asioita
		Vector3 direction = Transform.Basis * new Vector3(inputDir.Y, 0, inputDir.X);
		direction = direction.Normalized();		// Asetetaan vectorin suuruudeksi 1

		// Toteutetaan liike, jos direction on 0 niin pysäytetään pelaaja
		if (direction != Vector3.Zero) {
			tempVelocity.X = direction.X * moveSpeed * deltaF;
			tempVelocity.Z = direction.Z * moveSpeed * deltaF;
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
