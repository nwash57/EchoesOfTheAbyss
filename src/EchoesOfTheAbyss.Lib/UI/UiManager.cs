using System.Text;
using EchoesOfTheAbyss.Lib.Extensions;
using EchoesOfTheAbyss.Lib.Models;
using Spectre.Console;

namespace EchoesOfTheAbyss.Lib.UI;

public class UiManager
{
	private bool _waitingForInput = true;
	// private int _currentSelection = 0;
	// private string _textInput = string.Empty;
	private PanelEnum _activePanel = PanelEnum.Conversation;
	// private int _rightPanelSelection = 0; // Track selection in right panel

	// Right panel content state
	// private List<string> _rightPanelItems = new List<string>();
	// private bool _showingThoughts = false;
	
	public delegate void ConversationEnteredHandler(object sender, string text);
	public event ConversationEnteredHandler ConversationEntered;
	
	public Layout Layout = new Layout("Root")
		.SplitColumns(
			new Layout(PanelEnum.Conversation.GetDescription())
			{
				Size = (int)(Console.WindowWidth * 0.6)
			},
			new Layout(PanelEnum.Details.GetDescription())
			{
				Size = (int)(Console.WindowWidth * 0.4)
			} 
		);

	public UiManager()
	{
		_panels[PanelEnum.Conversation].UpdatePanel += OnConversationPanelUpdate;
		_panels[PanelEnum.Conversation].TextEntered += OnConversationEntered;
		_panels[PanelEnum.Details].UpdatePanel += OnDetailsPanelUpdate;
	}

	private void OnConversationEntered(object sender, string text)
	{
		ConversationEntered?.Invoke(this, text);
	}
	
	private void OnConversationPanelUpdate(object sender, Panel panel)
	{
		Layout[PanelEnum.Conversation.GetDescription()].Update(panel);
	}
	
	private void OnDetailsPanelUpdate(object sender, Panel panel)
	{
		Layout[PanelEnum.Details.GetDescription()].Update(panel);
	}

	

	private Dictionary<PanelEnum, UiPanel> _panels = new()
	{
		[PanelEnum.Conversation] = new ConversationUiPanel(),
		[PanelEnum.Details] = new DetailsUiPanel()
	};

	public UiLoopContext RunUiLoop(List<Message> messages, WorldContext worldContext)
	{
		Console.Clear();

		// Create the header that spans both panels
		AnsiConsole.Write(new Rule("[yellow]Echoes of the Abyss[/]").RuleStyle("grey").DoubleBorder());

		_panels[PanelEnum.Conversation].Update(new PanelData
		{
			IsActive = _activePanel == PanelEnum.Conversation,
			ExpandableSections = messages
		});
		
		var worldContextData = new PanelData
		{
			IsActive = _activePanel == PanelEnum.Details,
			ExpandableSections =
			[
				worldContext.Player,
				worldContext.CurrentLocation
			]
		};
		_panels[PanelEnum.Details].Update(worldContextData);
		
		// // Determine right panel header based on content type
		// string rightPanelHeader;
		//
		// if (_showingThoughts)
		// {
		// 	rightPanelHeader = _activePanel == PanelEnum.Details ? "[b][green]Thoughts[/][/]" : "[b]Thoughts[/]";
		//
		// 	if (_rightPanelItems.Count > 0)
		// 	{
		// 		for (int i = 0; i < _rightPanelItems.Count; i++)
		// 		{
		// 			if (i == _rightPanelSelection && _activePanel == PanelEnum.Details)
		// 			{
		// 				_panelBuilders[PanelEnum.Details].AppendLine($"[bold blue]{_rightPanelItems[i]}[/]");
		// 			}
		// 			else
		// 			{
		// 				_panelBuilders[PanelEnum.Details].AppendLine(_rightPanelItems[i]);
		// 			}
		// 		}
		// 	}
		// 	else
		// 	{
		// 		_panelBuilders[PanelEnum.Details].Append("No thoughts available for this message.");
		// 	}
		// }
		// else
		// {
		// 	rightPanelHeader = _activePanel == PanelEnum.Details
		// 		? "[b][green]World Context[/][/]"
		// 		: "[b]World Context[/]";
		// 	PrintExpandableGroups(
		// 		new[]
		// 		{
		// 			new ExpandableSection<Location>()
		// 			{
		// 				Content = worldContext.CurrentLocation,
		// 				Header =
		// 					$"Location: {worldContext.CurrentLocation.Coordinates} - {worldContext.CurrentLocation.ShortDescription}",
		// 				Details = worldContext.CurrentLocation.LongDescription.Split('\n').ToList()
		// 			}
		// 		},
		// 		PanelEnum.Details,
		// 		PanelEnum.Details);
		// }
		//
		// // Update right panel
		// layout["Details"].Update(
		// 	new Panel(_panelBuilders[PanelEnum.Details].ToString())
		// 		.Border(_activePanel == PanelEnum.Details ? BoxBorder.Double : BoxBorder.Rounded)
		// 		.BorderColor(_activePanel == PanelEnum.Details ? Color.Green : Color.Grey)
		// 		.Header(rightPanelHeader)
		// 		.Padding(1, 1, 1, 1)
		// );

		// Render the layout
		AnsiConsole.Write(Layout);

		// Handle keyboard input
		var keyInfo = Console.ReadKey(true);
		switch (keyInfo.Key)
		{
			case ConsoleKey.Tab:
				// Switch active panel and show world context when switching to right panel
				_activePanel = _activePanel == PanelEnum.Conversation ? PanelEnum.Details : PanelEnum.Conversation;
				// if (_activePanel == PanelEnum.Conversation)
				// {
				// 	_showingThoughts = false;
				// }

				break;


			// case ConsoleKey.RightArrow:
			// 	if (_activePanel == PanelEnum.Conversation && _currentSelection < messages.Count)
			// 	{
			// 		// Show thoughts in right panel
			// 		var selectedMessage = messages[_currentSelection];
			// 		_rightPanelItems = selectedMessage.Thoughts.ToList();
			// 		_rightPanelSelection = 0; // Reset right panel selection
			// 		_showingThoughts = true;
			// 	}
			//
			// 	break;

			// case ConsoleKey.LeftArrow:
			// 	// Show world context in right panel
			// 	_showingThoughts = false;
			// 	break;

			case ConsoleKey.Escape:
				_waitingForInput = false;
				break;


			default:
				_panels[_activePanel].HandleKeystroke(keyInfo);
				break;
		}

		return new UiLoopContext
		{
			// TextInput = _textInput,
			WaitingForInput = _waitingForInput
		};
	}
	
}