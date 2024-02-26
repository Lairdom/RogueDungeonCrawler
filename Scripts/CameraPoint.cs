using Godot;
using System;
using System.Diagnostics;

public partial class CameraPoint : Node3D
{
	private const float FOLLOWSPEED = 10f;		// < 1 really slow, cant keep up. > 10 smooth
	[Export] NodePath playerpath = null;
	private Node3D player = default;

	public void _OnPlayerInputEvent(Node kamera, InputEvent tapahtuma, Vector3 paikka, Vector3 normaali) {
		Debug.Print("Node: "+kamera+", Event: "+tapahtuma+", Position: "+paikka);
	}

	public override void _Ready() {
		player = GetNodeOrNull<Node3D>(playerpath);
	}

	
	public override void _Process(double delta) {
		float deltaF = (float) delta;

		//Position = player.Position;					// Teleportataan kamera pelaajan kohtaan

		// Jos tehdään smoothimpi 
		float posX = Mathf.MoveToward(Position.X, player.Position.X, FOLLOWSPEED * deltaF);
		float posY = Mathf.MoveToward(Position.Y, player.Position.Y, FOLLOWSPEED * deltaF);
		float posZ = Mathf.MoveToward(Position.Z, player.Position.Z, FOLLOWSPEED * deltaF);
		Position = new Vector3(posX, posY, posZ);
	}
}
