using Godot;
using System;
using System.Diagnostics;

public partial class GameManager : Node3D
{
	[Signal]
	public delegate void PlayerHealthChangedEventHandler(float currentHealth, float maxHealth);
	[Signal]
	public delegate void AllEnemiesDefeatedEventHandler();

	[ExportCategory("Player Stats")]
	[Export] public bool playerAlive = true;
	[Export] public int playerMaxHealth;
	[Export] public int playerHealth;
	[Export] public int attackPower;
	[Export] public float critChance;
	[Export] public float movementSpeed;
	[Export] public bool araknoPhobiaMode = false;
	public bool door1Unlocked, door2Unlocked, door3Unlocked = false;
	public int currentRoom = 0;
	public int numberOfEnemies, numberOfSpiders, numberOfOrbs, numberOfSkeletons, numberOfRangedSkeletons;
	public bool bossSpawned;
	Node3D root = default;
	Player player = default;
	PackedScene spider = ResourceLoader.Load("res://PrefabObjects/Enemy-Spider/enemy_spider.tscn") as PackedScene;
	PackedScene orb = ResourceLoader.Load("res://PrefabObjects/Enemy-Orb/EnemyOrb.tscn") as PackedScene;
	PackedScene skeleton = ResourceLoader.Load("res://PrefabObjects/Enemy-Skeleton/enemy_skeleton.tscn") as PackedScene;
	PackedScene rangedSkeleton = ResourceLoader.Load("res://PrefabObjects/Enemy-RangedSkeleton/Enemy_Ranged_Skeleton.tscn") as PackedScene;
	PackedScene boss = ResourceLoader.Load("res://PrefabObjects/Enemy-Boss/enemy_boss.tscn") as PackedScene;
	Vector3[] scaleSets = {new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1, 1, 1), new Vector3(1.5f, 1.5f, 1.5f)};

	// Set Stage for each room
	public async void SetStage() {
		await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
		
		if (currentRoom == 1) {
			numberOfSpiders = GD.RandRange(0,3);
			numberOfOrbs = GD.RandRange(0,1);	
			numberOfSkeletons = GD.RandRange(0,2);
			numberOfRangedSkeletons = GD.RandRange(0,2);
			numberOfEnemies = numberOfSpiders+numberOfOrbs+numberOfSkeletons+numberOfRangedSkeletons;
		}
		else if (currentRoom == 2) {
			numberOfSpiders = GD.RandRange(0,6);
			numberOfOrbs = GD.RandRange(0,3);
			numberOfSkeletons = GD.RandRange(0,4);
			numberOfRangedSkeletons = GD.RandRange(0,4);
			numberOfEnemies = numberOfSpiders+numberOfOrbs+numberOfSkeletons+numberOfRangedSkeletons;
		}
		else if (currentRoom == 3) {
			Vector3 point = GetNodeOrNull<Node3D>("/root/World/PatrolPositions/PatrolPoint16").GlobalPosition;
			SpawnBoss(point);
			numberOfEnemies++;
		}
		if (currentRoom < 3 && currentRoom != 0) {
			for (int i = 0; i<numberOfSpiders; i++) 
				SpawnSpider(SelectRandomSpawnPoint());
			for (int i = 0; i<numberOfOrbs; i++) 
				SpawnOrb(SelectRandomSpawnPoint());
			for (int i = 0; i<numberOfSkeletons; i++)
				SpawnSkeleton(SelectRandomSpawnPoint());
			for (int i = 0; i<numberOfRangedSkeletons; i++)
				SpawnRangedSkeleton(SelectRandomSpawnPoint());
		}
		if (numberOfEnemies == 0 && currentRoom != 0)
			SetStage();
	}

	// Spawn Spider
	public void SpawnSpider(Vector3 location) {
		EnemySpider spiderInstance = (EnemySpider) spider.Instantiate();
		spiderInstance.Scale = scaleSets[GD.RandRange(0,2)];
		location.Y = 0.25f;
		spiderInstance.Position = location;
		root.AddChild(spiderInstance);
		spiderInstance.statHandler.AddToGroup("enemies");
	}

	// Spawn Orb
	public void SpawnOrb(Vector3 location) {
		EnemyOrb orbInstance = (EnemyOrb) orb.Instantiate();
		location.Y = 0.65f;
		orbInstance.Position = location;
		root.AddChild(orbInstance);
		orbInstance.statHandler.AddToGroup("enemies");
	}

	// Spawn Skeleton
	public void SpawnSkeleton(Vector3 location) {
		EnemySkeleton skeletonInstance = (EnemySkeleton) skeleton.Instantiate();
		location.Y = 0.5f;
		skeletonInstance.Position = location;
		root.AddChild(skeletonInstance);
		skeletonInstance.statHandler.AddToGroup("enemies");
	}

	// Spawn Skeleton
	public void SpawnRangedSkeleton(Vector3 location) {
		EnemyRangedSkeleton rangedSkelInstance = (EnemyRangedSkeleton) rangedSkeleton.Instantiate();
		location.Y = 0.5f;
		rangedSkelInstance.Position = location;
		root.AddChild(rangedSkelInstance);
		rangedSkelInstance.statHandler.AddToGroup("enemies");
	}

	// Spawn Boss
	public void SpawnBoss(Vector3 location) {
		EnemyBoss bossInstance = (EnemyBoss) boss.Instantiate();
		location.Y = 1.5f;
		bossInstance.Position = location;
		root.AddChild(bossInstance);
		bossInstance.statHandler.AddToGroup("enemies");
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
		// Unlock the door to the next room
		if (currentRoom == 0)
			door1Unlocked = true;
		else if (currentRoom == 1)
			door2Unlocked = true;
			
		currentRoom++;
		SetStage();
	}

	// Signaali saadaan aina kun vihollinen tapetaan
	public void CheckAllEnemiesDefeated() {
		bool allDefeated = true; // Assume all enemies are defeated initially
	
		// Iterate through all enemies in the scene
		foreach (EnemyStats enemy in GetTree().GetNodesInGroup("enemies")) {
			// Check if the enemy is still alive
			if (enemy.isAlive) {
				// If any enemy is still alive, set the flag to false and break the loop
				allDefeated = false;
				break;
			}
		}
		
		// If all enemies are defeated, emit the signal
		if (allDefeated && currentRoom != 2) {
			Debug.Print("All defeated");
			RoomCleared();
			EmitSignal(nameof(AllEnemiesDefeated));
		}
		else if (allDefeated && currentRoom == 2) {
			RoomCleared();
		}
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

	// Kutsutaan kun pelaaja tekee vahinkoa viholliseen
	public float CheckCrit() {
		float critModifier = 1f;
		float chance = critChance*100;
		bool crit = GD.RandRange(1, 100) < chance;
		if (crit)
			critModifier = 3f;
		return critModifier;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		root = GetNodeOrNull<Node3D>("/root/World");
		player = GetNodeOrNull<Player>("/root/World/Player");
		SetStage();
	}
}
