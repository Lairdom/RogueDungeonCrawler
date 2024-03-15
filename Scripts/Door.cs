using Godot;
using System;
using System.Diagnostics;

public partial class Door : Node3D
{
	[Export] int connectedRoom; 
	GameManager GM;
	private AnimationPlayer animationPlayer;
	private bool isOpening = false; // Track if the door is currently opening
	bool playerInRange = false;
	bool doorOpen = false;
	bool doorUnlocked;
	float animationTimer;
	AudioStreamPlayer3D audioSource = default;
	AudioStreamOggVorbis openSound = ResourceLoader.Load("res://Audio/SoundEffects/DoorCreak.ogg") as AudioStreamOggVorbis;
	AudioStreamOggVorbis closeSound = ResourceLoader.Load("res://Audio/SoundEffects/DoorClose1.ogg") as AudioStreamOggVorbis;

	//Signaali joka saadaan kun pelaaja astuu PlayerDetector colliderin alueelle
	private void OnPlayerEntered(Node3D body) {
		if (body.Name == "Player") {
			playerInRange = true;
		}
	}

	//Signaali joka saadaan kun pelaaja poistuu PlayerDetector colliderin alueelta
	private void OnPlayerExited(Node3D body) {
		if (body.Name == "Player") {
			playerInRange = false;
		}
	}

	// Check if door is set to unlocked in GameManager
	private void CheckIfDoorUnlocked() {
		if (connectedRoom == 1)
			doorUnlocked = GM.door1Unlocked;
		if (connectedRoom == 2)
			doorUnlocked = GM.door2Unlocked;
		if (connectedRoom == 3)
			doorUnlocked = GM.door3Unlocked;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		GM = GetNodeOrNull<GameManager>("/root/World/GameManager");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		audioSource = GetNode<AudioStreamPlayer3D>("AudioPlayer");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (playerInRange && Input.IsActionJustPressed("Examine")) {
			CheckIfDoorUnlocked();
			if (!doorOpen && doorUnlocked) {
				if (!animationPlayer.IsPlaying()) {
					animationTimer = 0;
					animationPlayer.Play("open");
					audioSource.Stream = openSound;
					audioSource.Play();
				}
			}
			else if (!doorOpen && !doorUnlocked) {
				Debug.Print("Door is Locked");
			}
			else if (doorOpen) {
				if (!animationPlayer.IsPlaying()) {
					animationTimer = 0;
					animationPlayer.Play("close");
					audioSource.Stream = closeSound;
					audioSource.Play();
				}
			}
		}
		if (animationPlayer.IsPlaying()) {
			if (doorOpen && animationTimer >= animationPlayer.CurrentAnimationLength)
				doorOpen = false;
			else if (!doorOpen && animationTimer >= animationPlayer.CurrentAnimationLength)
				doorOpen = true;
			animationTimer += delta;
		}
	}
}
