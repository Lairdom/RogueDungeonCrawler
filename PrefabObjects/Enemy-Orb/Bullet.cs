using Godot;
using System;
using System.Diagnostics;

public partial class Bullet : Area3D
{
	float timer = 5;
	

	private void OnHit(Node3D body) {
		//Free(); 		// Deletes node immediately
		QueueFree();	// Deletes Node after all its DeferredCalls have ended
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		Vector3 direction = Transform.Basis * Vector3.Forward;
		Position += direction * 5 * delta;
		timer -= delta;
		if (timer <= 0)
			QueueFree();
	}
}
