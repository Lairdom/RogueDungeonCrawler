using Godot;
using System;

public partial class PassiveSkills : Node
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

    private void ApplySkillEffects(PassiveSkill skill)
    {
        if (skill.Name == "Movement Speed Up")
             player.moveSpeed += 10;
        // crit rate is not a thing yet
        // else if (skill.Name == "Critical Rate Up")
        //     player.CriticalRate += 0.05f;
    }

    private void PresentSkillSelection(PassiveSkill[] skills)
    {
        // Display the skills and allow the player to choose one
    }

    private void HandleSkillSelection(PassiveSkill selectedSkill)
    {
        ApplySkillEffects(selectedSkill);
        // Update UI to display information about the selected skill
    }

    private void OnEnemiesDefeated()
    {
        PassiveSkill[] randomSkills = SelectRandomSkills();
        PresentSkillSelection(randomSkills);
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
        // Subscribe to the event for when all enemies in a room are defeated
    }
}