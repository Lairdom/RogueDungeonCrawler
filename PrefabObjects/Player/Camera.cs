using Godot;
using System;
using System.Diagnostics;

public partial class Camera : Node3D
{
	float camRotH = 0;				// CameraRotationHorizontal
	float camRotV = 0;				// CameraRotationVertical
	Node3D H = default;
	Node3D V = default;
	Node3D player = default;
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
				Debug.Print("Reached max");
			else if (camRotV >= 90 && mouse.Relative.Y < 0)
				Debug.Print("Reached min");
			else
				camRotV += -mouse.Relative.Y;
		}
    }

    public override void _Ready() {
		player = GetParent<Node3D>();
		H = GetNode<Node3D>("Horizontal");
		V = GetNode<Node3D>("Horizontal/Vertical");
		
	}

	public override void _Process(double delta) {
		// Pyöritetään pelaajaa tai kameraa
		player.RotationDegrees = new Vector3(0,camRotH, 0);
		//H.RotationDegrees = new Vector3(0, camRotH, 0);
		V.RotationDegrees = new Vector3(0, 0, camRotV);
	}
}
