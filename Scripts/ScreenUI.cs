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

    public override void _Ready(){
        // Find UI elements by their names
        healthBar = GetNode<ProgressBar>("HealthBar");
        stanceLabel = GetNode<Label>("StanceLabel");
        weaponLabel = GetNode<Label>("WeaponLabel");
        passiveSkillsLabel = GetNode<Label>("PassiveSkillsLabel");

        // Initialize UI elements
        InitializeUI();

        // Connect to the signal emitted by GameManager
        GetNode<GameManager>("/root/World/GameManager").Connect(nameof(GameManager.PlayerHealthChangedEventHandler), new Callable(this, nameof(OnPlayerHealthChanged)));
    }

    private void InitializeUI() {
        // Set initial values for UI elements
        healthBar.Value = 100; // Example health value
        stanceLabel.Text = "Slash"; // Example stance
        weaponLabel.Text = "Sword"; // Example weapon
        passiveSkillsLabel.Text = "None"; // Example passive skills
    }

    // Update health bar
    public void UpdateHealthBar(float currentHealth, float maxHealth) {
        healthBar.Value = Mathf.Clamp(currentHealth / maxHealth, 0f, 1f) * 100f;
    }

    // Update stance label
    public void UpdateStance(string newStance) {
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

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}