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

    Node3D player = default;

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
        player = GetNodeOrNull<Player>("/root/World/Player");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double dDelta) {

    }
}
