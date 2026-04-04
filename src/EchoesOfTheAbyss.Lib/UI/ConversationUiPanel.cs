using EchoesOfTheAbyss.Lib.Extensions;
using Spectre.Console;

namespace EchoesOfTheAbyss.Lib.UI;

public class ConversationUiPanel : UiPanel
{
	public ConversationUiPanel() : base(PanelEnum.Conversation.GetDescription())
	{}

	public override void Update(PanelData panelData)
	{
		PanelData = panelData;
		PanelBuilder.Clear();
		PrintExpandableGroups(panelData.ExpandableSections, expandInSeparatePanel: false);
		
		PanelBuilder.AppendLine();
		PanelBuilder.Append($"[yellow]> [/][green]{TextInput}[/]");
		PanelBuilder.AppendLine();
		PanelBuilder.AppendLine(
			"[grey]↑/↓ Navigate | ←/→ Show/Hide Thoughts | Tab: Change Active Panel | Esc: Exit[/]");

		OnUpdatePanel(BuildStandardPanel());
	}

	public override Dictionary<ConsoleKey, Action<ConsoleKeyInfo>> KeyBindings => new()
	{
		[ConsoleKey.UpArrow] = OnSelectionUp,
		[ConsoleKey.DownArrow] = OnSelectionDown,
		[ConsoleKey.LeftArrow] = OnMessageExpanded,
		[ConsoleKey.RightArrow] = OnMessageExpanded,
		[ConsoleKey.Backspace] = OnTextBackspaced,
		[ConsoleKey.Enter] = _ => { OnTextEntered(TextInput); },
	};

	public override void HandleKeystroke(ConsoleKeyInfo keyInfo)
	{
		KeyBindings.TryGetValue(keyInfo.Key, out var action);
		if (action != null)
		{
			action.Invoke(keyInfo);
		}
		else
		{
			TextInput += keyInfo.KeyChar;
		}
	}
	
	
}