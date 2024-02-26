using Godot;
using System;
using System.Diagnostics;

public partial class Player2 : CharacterBody3D
{
	public const float SPEED = 5.0f;
	public const float JUMPVELOCITY = 4.5f;
	public float gravity = (float) ProjectSettings.GetSetting("physics/3d/default_gravity");

	public override void _Ready() {
		Debug.Print(this.Name);		// Tulostaa tämän objektin nimen, this ei pakollinen.
	}

	
	public override void _Process(double delta) {
		float deltaF = (float) delta;

		Vector3 velocity = Velocity;

		if (IsOnFloor() == false) {
			velocity.Y -= gravity * deltaF;
		}

		// Liike
		//Vector2 playerInput = new Vector2(Input.GetAxis("MoveLeft", "MoveRight"), Input.GetAxis("MoveDown", "MoveUp"));			// Tapa 1
		Vector2 playerInput = Input.GetVector("MoveLeft", "MoveRight", "MoveDown", "MoveUp");									    // Tapa 2
		Vector3 direction = Transform.Basis * new Vector3(playerInput.Y, 0, playerInput.X);
		direction = direction.Normalized();
		/*
		// Toteutetaan liike muuttamalla paikkaa (teleport liikkuminen)
		if (playerInput.Y > 0) {
			// Move Player Up, note: X-axel is the north-south axel
			Position += Transform.Basis.X * SPEED * deltaF;
		}
		else if (playerInput.Y < 0) {
			// Move Player Down, note: X-axel is the north-south axel
			Position -= Transform.Basis.X * SPEED * deltaF;
		}
		if (playerInput.X > 0) {
			// Move Player Right, note: Z-axel is the east-west axel
			Position += Transform.Basis.Z * SPEED * deltaF;
		}
		else if (playerInput.X < 0) {
			// Move Player Left, note: Z-axel is the east-west axel
			Position -= Transform.Basis.Z * SPEED * deltaF;
		}
		*/
		// Toteutetaan liike muuttamalla Velocity arvoa
		if (direction != Vector3.Zero) {
			velocity.X = direction.X * SPEED;
			velocity.Z = direction.Z * SPEED;
		}
		else {
			velocity.X = Mathf.MoveToward(Velocity.X, 0, SPEED);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, SPEED);
		}
		
		Velocity = velocity;
		MoveAndSlide();
	}
}
