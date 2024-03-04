using Godot;
using System;

public partial class ScreenUI : CanvasLayer
{
    // Health bar
    ProgressBar healthBar;

    // Stance indication
    Label stanceLabel;

    // Current weapon
    Label weaponLabel;

    // Passive skills
    Label passiveSkillsLabel;

	public override void _Ready(){
		// Find UI elements by their names
        healthBar = GetNode<ProgressBar>("HealthBar");
        stanceLabel = GetNode<Label>("StanceLabel");
        weaponLabel = GetNode<Label>("WeaponLabel");
        passiveSkillsLabel = GetNode<Label>("PassiveSkillsLabel");

        // Initialize UI elements
        InitializeUI();
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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
