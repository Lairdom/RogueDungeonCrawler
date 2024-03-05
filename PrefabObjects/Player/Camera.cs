using Godot;
using System;
using System.Diagnostics;

public partial class Camera : Node3D
{
	GameManager GM;
	float camRotH = 0;				// CameraRotationHorizontal
	float camRotV = 0;				// CameraRotationVertical
	Node3D H = default;
	Node3D V = default;
	Node3D player = default;
	RayCast3D cameraCollider = default;
	Camera3D cam = default;
	Node3D camPos = default;
	public bool lookMode = false;

	// Otetaan vastaan inputEvent. _Input on Godotin sisäänrakennettu funktio
    public override void _Input(InputEvent tapahtuma) {
		// Tarkastetaan että event on hiiren liikkuminen
		if (tapahtuma is InputEventMouseMotion) {
			InputEventMouseMotion mouse = (InputEventMouseMotion) tapahtuma;

			//Kamera saa pyöriä horizontal suunnassa vapaasti
			camRotH += -mouse.Relative.X;

			//Estetään kameran pyöriminen jos menee yli 90 tai -90 astetta
			if (camRotV <= -90 && mouse.Relative.Y > 0)
				return;
			else if (camRotV >= 90 && mouse.Relative.Y < 0)
				return;
			else
				camRotV += -mouse.Relative.Y;
		}
    }

    public override void _Ready() {
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		player = GetParent<Player>();
		H = GetNode<Node3D>("Horizontal");
		V = GetNode<Node3D>("Horizontal/Vertical");
		cameraCollider = GetNode<RayCast3D>("Horizontal/Vertical/CollisionDetection");
		cam = GetNode<Camera3D>("Horizontal/Vertical/Camera");
		camPos = GetNode<Node3D>("Horizontal/Vertical/CameraPos");
	}

	public override void _Process(double delta) {
		if (GM.playerAlive) {
			// Kameran Collision Detection
			if (cameraCollider.IsColliding()) {
				Vector3 collisionPoint = cameraCollider.GetCollisionPoint();
				cam.GlobalPosition = cam.GlobalPosition.Lerp(collisionPoint, 1f);
			}
			else
				cam.GlobalPosition = camPos.GlobalPosition;
			// Pyöritetään pelaajaa tai kameraa
			player.RotationDegrees = new Vector3(0,camRotH, 0);
			//H.RotationDegrees = new Vector3(0, camRotH, 0);
			V.RotationDegrees = new Vector3(0, 0, camRotV);
		}
	}
}
