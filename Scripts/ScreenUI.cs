using Godot;
using System;

public partial class ScreenUI : CanvasLayer
{
	// Health bar
	[Export] public ProgressBar healthBar;
	// Stance indication
	[Export] public Label stanceLabel;
	// Current weapon
	[Export] public Label weaponLabel;
	// Passive skills
	[Export] public Label passiveSkillsLabel;
	private string[] stanceTexts = { "Slashing", "Piercing", "Bludgeoning" };
	// private Player player; (not in use at the moment)

	private void InitializeUI() {
		// Set initial values for UI elements
		healthBar.Value = 100; // Example health value
		// this doesn't work as intended, label goes from slashing to piercing to back to slashing and then starts to work normally
		UpdateStanceLabel(stanceTexts[0]); // Initialize stance label with the first stance
		// and this commented version breaks the whole UI
		// UpdateStanceLabel(player.stance[0]); // Initialize stance label with the first stance from Player.cs
		weaponLabel.Text = "Sword"; // Example weapon
		passiveSkillsLabel.Text = "None"; // Example passive skills
	}

	// Update health bar
	public void UpdateHealthBar(float currentHealth, float maxHealth) {
		healthBar.Value = Mathf.Clamp(currentHealth / maxHealth, 0f, 1f) * 100f;
	}

	// Update stance label
	public void UpdateStanceLabel(string newStance) {
		stanceLabel.Text = newStance;
	}

	// Update weapon label
	public void UpdateWeapon(string newWeapon) {
		weaponLabel.Text = newWeapon;
	}

	// Update passive skills label
	public void UpdatePassiveSkills(string newSkills) {
		passiveSkillsLabel.Text = newSkills;
	}

	// Called when player health changes
	private void OnPlayerHealthChanged(float currentHealth, float maxHealth) {
		UpdateHealthBar(currentHealth, maxHealth);
	}

	private void OnStanceChanged(string newStance)
    {
        // Update the UI to reflect the new stance
        stanceLabel.Text = newStance;
    }

	public override void _Ready(){
		// Find UI elements by their names
		healthBar = GetNode<ProgressBar>("HealthBar");
		stanceLabel = GetNode<Label>("StanceLabel");
		weaponLabel = GetNode<Label>("WeaponLabel");
		passiveSkillsLabel = GetNode<Label>("PassiveSkillsLabel");

		// Initialize UI elements
		InitializeUI();

		// Connect to the signal emitted by GameManager
		GetNode<GameManager>("/root/World/GameManager").Connect(nameof(GameManager.PlayerHealthChanged), new Callable(this, nameof(OnPlayerHealthChanged)));
		GetNode<Player>("/root/World/Player").Connect(nameof(Player.StanceChanged), new Callable(this, nameof(OnStanceChanged)));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
