using EchoesOfTheAbyss.Lib.Extensions;
using EchoesOfTheAbyss.Lib.Models;
using Spectre.Console;

namespace EchoesOfTheAbyss.Lib.UI;

public class DetailsUiPanel : UiPanel
{
	public DetailsUiPanel() : base(PanelEnum.Details.GetDescription())
	{
	}

	public override void Update(PanelData panelData)
	{
		PanelData = panelData;
		PanelBuilder.Clear();

		var playerSection = panelData.ExpandableSections.OfType<Player>().FirstOrDefault();
		if (playerSection != null)
		{
			var equipment = panelData.ExpandableSections.OfType<Equipment>().FirstOrDefault();
			
			// Health Bar
			var health = playerSection.Stats.CurrentHealth;
			var maxHealth = playerSection.Stats.MaxHealth;
			var healthColor = health > 50 ? "green" : health > 20 ? "yellow" : "red";
			
			PanelBuilder.AppendLine($"Health: [{healthColor}]{health}/{maxHealth}[/]");
			var barWidth = 20;
			var filledWidth = (int)Math.Round((double)health / maxHealth * barWidth);
			var bar = new string('█', filledWidth) + new string('░', barWidth - filledWidth);
			PanelBuilder.AppendLine($"[{healthColor}]{bar}[/]");
			PanelBuilder.AppendLine();

			// Stats (Armor and Strength)
			var totalArmor = playerSection.Stats.BaseArmor + (equipment != null ? 
				(equipment.Head?.Armor ?? 0) + (equipment.Torso?.Armor ?? 0) + (equipment.Legs?.Armor ?? 0) + (equipment.Feet?.Armor ?? 0) + (equipment.RightHand?.Armor ?? 0) + (equipment.LeftHand?.Armor ?? 0) : 0);
			
			var totalStrength = playerSection.Stats.BaseStrength + (equipment != null ? 
				(equipment.Head?.Damage ?? 0) + (equipment.Torso?.Damage ?? 0) + (equipment.Legs?.Damage ?? 0) + (equipment.Feet?.Damage ?? 0) + (equipment.RightHand?.Damage ?? 0) + (equipment.LeftHand?.Damage ?? 0) : 0);

			PanelBuilder.AppendLine($"[blue]Armor:[/] {totalArmor} [blue]Strength:[/] {totalStrength}");
			PanelBuilder.AppendLine(new string('─', barWidth + 2));
		}

		PrintExpandableGroups(panelData.ExpandableSections, expandInSeparatePanel: false);
		PanelBuilder.AppendLine();
		PanelBuilder.AppendLine(
			"[grey]↑/↓ Navigate | ←/→ Show/Hide details | Tab: Change Active Panel[/]");
		OnUpdatePanel(BuildStandardPanel());
	}

	public override Dictionary<ConsoleKey, Action<ConsoleKeyInfo>> KeyBindings => new()
	{
		[ConsoleKey.UpArrow] = OnSelectionUp,
		[ConsoleKey.DownArrow] = OnSelectionDown,
		[ConsoleKey.LeftArrow] = OnMessageExpanded,
		[ConsoleKey.RightArrow] = OnMessageExpanded,
	};
}