using Godot;
using System;
using System.Diagnostics;

public partial class GameManager : Node3D
{
	[Signal]
	public delegate void PlayerHealthChangedEventHandler(float currentHealth, float maxHealth);

	[ExportCategory("Player Stats")]
	[Export] public bool playerAlive = true;
	[Export] public int playerMaxHealth;
	[Export] public int playerHealth;
	[Export] public int attackPower;
	[Export] public float movementSpeed;
	[Export] public bool araknoPhobiaMode = false;
	public int currentRoom = 0;
	public int numberOfEnemies;
	public int numberOfSpiders;
	public int numberOfOrbs;
	Node3D root = default;
	Player player = default;
	PackedScene spider = ResourceLoader.Load("res://PrefabObjects/Enemy-Spider/enemy_spider.tscn") as PackedScene;
	PackedScene orb = ResourceLoader.Load("res://PrefabObjects/Enemy-Orb/EnemyOrb.tscn") as PackedScene;
	

	// Set Stage for each room
	public void SetStage() {
		if (currentRoom == 0) {
			numberOfEnemies = 0;
		}
		else if (currentRoom == 1) {
			numberOfSpiders = GD.RandRange(0,3);
			numberOfOrbs = GD.RandRange(0,2);			
		}
		else if (currentRoom == 2) {
			numberOfSpiders = GD.RandRange(0,5);
			numberOfOrbs = GD.RandRange(0,4);
		}
		for (int i = 0; i<numberOfSpiders; i++) {
			SpawnSpider(SelectRandomSpawnPoint());
		}
		for (int i = 0; i<numberOfOrbs; i++) {
			SpawnOrb(SelectRandomSpawnPoint());
		}
		Debug.Print("Number of Spiders: "+numberOfSpiders+", number of Orbs: "+numberOfOrbs);
		numberOfEnemies = numberOfSpiders+numberOfOrbs;
		if (numberOfEnemies == 0 && currentRoom != 0)
			SetStage();
	}

	// Spawn Spider
	public void SpawnSpider(Vector3 location) {
		EnemySpider spiderInstance = (EnemySpider) spider.Instantiate();
		spiderInstance.Position = location;
		root.AddChild(spiderInstance);
	}

	// Spawn Orb
	public void SpawnOrb(Vector3 location) {
		EnemyOrb orbInstance = (EnemyOrb) orb.Instantiate();
		orbInstance.Position = location;
		root.AddChild(orbInstance);
	}

	private Vector3 SelectRandomSpawnPoint() {
		string path;
		Vector3 randomPoint;
		int point = 0;
		if (currentRoom == 0)
			point = GD.RandRange(1,5);
		else if (currentRoom == 1)
			point = GD.RandRange(6,10);
		else if (currentRoom == 2)
			point = GD.RandRange(11,18);
		path = "/root/World/PatrolPositions/PatrolPoint"+point;
		randomPoint = GetNodeOrNull<Node3D>(path).GlobalPosition;
		return randomPoint;
	}

	// Called when room is cleared
	public void RoomCleared() {
		// Spawn 2 powerups or items from which the player can choose only 1
		Debug.Print("Room Cleared");
		currentRoom++;
		SetStage();
	}

	// Kutsutaan kun pelaaja ottaa vahinkoa tai parantaa itseään (vahinko on negatiivinen arvo, parantaminen positiivinen arvo)
	public void ChangePlayerHealth(int amount) {
		playerHealth += amount;
		if (playerHealth > playerMaxHealth)
			playerHealth = playerMaxHealth;
		if (playerHealth <= 0) {
			player.CallDeferred("PlayerDeath");
			playerAlive = false;
		}

		// Emit the signal to notify UI about the health change
		EmitSignal(SignalName.PlayerHealthChanged, playerHealth, playerMaxHealth);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		SetStage();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dDelta) {
		float delta = (float) dDelta;
		if (numberOfEnemies == 0) {
			RoomCleared();
		}

	}
}
