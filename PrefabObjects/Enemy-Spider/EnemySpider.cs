using Godot;
using System;
using System.Diagnostics;

public partial class EnemySpider : CharacterBody3D
{
	GameManager GM;
	Player player;
	Node3D root;
	Vector3 playerDirection;
	EnemyStats statHandler;
	bool isAlive = true;
	float playerDistance;
	NavigationAgent3D pathFinder;
	Vector3 movementTarget;
	float moveSpeed;
	bool playerDetected = false;
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public Vector3 MovementTarget {
		get { return pathFinder.TargetPosition; }
		set { pathFinder.TargetPosition = value; }
	}

	private async void MovementSetup() {
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		MovementTarget = movementTarget;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		pathFinder = GetNode<NavigationAgent3D>("Pathfinding");
		statHandler = GetNode<EnemyStats>("EnemyHandler");
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		if (GM == null || root == null || player == null) {
			Debug.Print("Null value");
		}
		moveSpeed = statHandler.movementSpeed;
		pathFinder.PathDesiredDistance = 0.2f;
		pathFinder.TargetDesiredDistance = 0.2f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (isAlive) {
			Vector3 tempVelocity = Velocity;
			if (!IsOnFloor())
				tempVelocity.Y -= gravity * delta;
			playerDirection = (player.GlobalPosition - GlobalPosition).Normalized();
			playerDistance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (playerDistance < 5) {
				playerDetected = true;
			}
			// Detects player
			if (playerDetected == true) {
				LookAt(player.GlobalPosition);
				movementTarget = player.GlobalPosition;
				if (playerDistance < 0.5f) {
					Debug.Print("Perform attack");
					return;
				}
			}
			// Moving around
			else {
				// Random Movement here
				if (pathFinder.IsNavigationFinished()) {
					return;
				}
			}
			Callable.From(MovementSetup).CallDeferred();
			Vector3 currentPosition = GlobalPosition;
			Vector3 nextPathPosition = pathFinder.GetNextPathPosition();
			Velocity = currentPosition.DirectionTo(nextPathPosition) * moveSpeed * delta;
			MoveAndSlide();
		}
	}
}
