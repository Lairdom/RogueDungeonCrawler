using Godot;
using System;

public partial class PassiveSkills : Node3D
{
[Export]
    private PackedScene passiveSkillPickupScene; // Reference to the passive skill pickup scene
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

    private void ApplySkillEffects(PassiveSkill skill)
    {
        if (skill.Name == "Movement Speed Up")
             player.moveSpeed += 10;
        // crit rate is not a thing yet
        // else if (skill.Name == "Critical Rate Up")
        //     player.CriticalRate += 0.05f;
    }
    // Player gets to pick a passive skill from UI popup
    private void PresentSkillSelection(PassiveSkill[] skills)
    {
        // Create a Popup dialog node
        Popup popup = new Popup();
        AddChild(popup); // Add the popup as a child of your scene

        // Create buttons for each skill
        foreach (PassiveSkill skill in skills)
        {
            Button button = new Button();
            button.Text = skill.Name; // Set the button text to the skill name
            // next line causes problems
            // button.Connect("pressed", this, nameof(OnSkillButtonPressed), new Godot.Collections.Array { skill.Name });
            popup.AddChild(button); // Add the button to the popup
        }

        // Display the popup
        popup.PopupCentered();
    }
    private void OnSkillButtonPressed(string skillName)
    {
        // Retrieve the skill information based on the skill name
        PassiveSkill selectedSkill = Array.Find(passiveSkills, skill => skill.Name == skillName);

        // Handle the selection of the skill here
        ApplySkillEffects(selectedSkill);
        GD.Print("Selected skill: " + selectedSkill.Name);
        GetNode<Popup>("Popup").QueueFree(); // Close the popup
        // Update UI to display information about the selected skill
        GetNode<ScreenUI>("/root/ScreenUI").UpdatePassiveSkills(selectedSkill.Name);
    }

  /*
    private void PresentSkillSelection(PassiveSkill[] skills)
    {
        // Spawn passive skill pickups for each skill
        foreach (PassiveSkill skill in skills)
        {
            // Instantiate the passive skill pickup scene
            // next line causes problems
            passiveskillpickup pickupInstance = (passiveskillpickup)passiveSkillPickupScene.Instance();

            // Set properties of the pickup instance based on the skill
            pickupInstance.Setup(skill.Name, skill.Description);

            // Add the pickup to the game world (adjust position as needed)
            AddChild(pickupInstance);
        }
    }

    private void OnSkillButtonPressed(PassiveSkill skill)
    {
        // Handle the selection of the skill here
        ApplySkillEffects(skill);
        GD.Print("Picked up skill: " + skill.Name);
        // Sound or visual effect here?
        // and also update UI
    }
  */
    private void OnEnemiesDefeated()
    {
        PassiveSkill[] randomSkills = SelectRandomSkills();
        PresentSkillSelection(randomSkills);

        // Check if all enemies are defeated
        GetNode<GameManager>("/root/World/GameManager").CheckAllEnemiesDefeated();
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