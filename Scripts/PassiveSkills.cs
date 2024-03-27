using Godot;
using System;
using System.Diagnostics;

public partial class PassiveSkills : Node3D
{
	private Player player;
	public class PassiveSkill
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}

	private PassiveSkill[] passiveSkills = new PassiveSkill[]
	{
		new PassiveSkill { Name = "Movement Speed Up", Description = "Increase movement speed by 10%" },
		new PassiveSkill { Name = "Critical Rate Up", Description = "Increase critical rate by 5%" }
		// Let's add more passive skills as needed
	};

	private PassiveSkill[] SelectRandomSkills()
	{
		Shuffle(passiveSkills);
		return new PassiveSkill[] { passiveSkills[0], passiveSkills[1] };
	}

		private void OnEnemiesDefeated()
	{
		Debug.Print("Enemies defeated");
		PassiveSkill[] randomSkills = SelectRandomSkills();
		PresentSkillSelection(randomSkills);

		// Check if all enemies are defeated
		GetNode<GameManager>("/root/World/GameManager").CheckAllEnemiesDefeated();
	}
	
	// Player gets to pick a passive skill from UI popup
	private void PresentSkillSelection(PassiveSkill[] skills)
	{
		// Pause the game
		GetTree().Paused = true;

		Debug.Print("Present skill selection popup");

		Input.MouseMode = Input.MouseModeEnum.Visible;

		// Create a Popup dialog node
		Popup popup = new Popup();
		AddChild(popup); // Add the popup as a child of your scene

		// Create a HBoxContainer to arrange buttons horizontally
		HBoxContainer hbox = new HBoxContainer();
		popup.AddChild(hbox);

		// Create buttons for each skill
		foreach (PassiveSkill skill in skills)
		{
			Button button = new Button();
			button.Text = skill.Name; // Set the button text to the skill name
			button.Name = skill.Name; // Set the button name to the skill name
			button.Connect("pressed", new Callable (this, nameof(OnSkillButtonPressed)));
			hbox.AddChild(button); // Add the button to the HBoxContainer
		}

		// Display the popup
		popup.PopupCentered();
	}

	private void OnSkillButtonPressed(string skillName)
	{
		Debug.Print("Selected skill button pressed");
		
		// Retrieve the skill information based on the skill name
		PassiveSkill selectedSkill = Array.Find(passiveSkills, skill => skill.Name == skillName);

		// Handle the selection of the skill here
		ApplySkillEffects(selectedSkill);
		GD.Print("Selected skill: " + selectedSkill.Name);

		// Unpause the game
		GetTree().Paused = false;

		// Hide the mouse
		Input.MouseMode = Input.MouseModeEnum.Hidden;

		GetNode<Popup>("Popup").QueueFree(); // Close the popup
		// Update UI to display information about the selected skill
		GetNode<ScreenUI>("/root/ScreenUI").UpdatePassiveSkills(selectedSkill.Name);
	}

		private void ApplySkillEffects(PassiveSkill skill)
	{
		if (skill.Name == "Movement Speed Up")
			 player.moveSpeed += 10;
		else if (skill.Name == "Critical Rate Up")
		{
			// Assuming critChance is an exported float in the GameManager script
			GameManager gameManager = GetNode<GameManager>("/root/World/GameManager");
			gameManager.critChance += 0.05f;
		}
	}

	private void Shuffle<T>(T[] array)
	{
		Random rng = new Random();
		int n = array.Length;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			T value = array[k];
			array[k] = array[n];
			array[n] = value;
		}
	}

	public override void _Ready()
	{
		// Connect to the signal emitted by the GameManager when all enemies are defeated
		GetNode<GameManager>("/root/World/GameManager").Connect(nameof(GameManager.AllEnemiesDefeated), new Callable (this, nameof(OnEnemiesDefeated)));
	}
}
