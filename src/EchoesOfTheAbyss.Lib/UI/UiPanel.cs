using System.Text;
using Spectre.Console;

namespace EchoesOfTheAbyss.Lib.UI;

public abstract class UiPanel
{
	protected PanelData PanelData { get; set; }
	protected readonly string Header = string.Empty;
	protected readonly StringBuilder PanelBuilder = new ();
	protected string TextInput { get; set; } = string.Empty;
	protected int CurrentSelection { get; set; } = 0;

	public delegate void UpdatePanelHandler(object sender, Panel panel);
	public event UpdatePanelHandler UpdatePanel;
	
	public delegate void TextEnteredHandler(object sender, string text);
	public event TextEnteredHandler TextEntered;

	public virtual void OnUpdatePanel(Panel panel)
	{
		UpdatePanel?.Invoke(this, panel);
	}
	
	protected virtual void OnTextEntered(string text)
	{
		TextEntered?.Invoke(this, text);
		TextInput = string.Empty;
	}

	protected UiPanel(string header)
	{
		Header = header;
	}

	public abstract void Update(PanelData panelData);
	public abstract Dictionary<ConsoleKey, Action<ConsoleKeyInfo>> KeyBindings { get; }

	public virtual void HandleKeystroke(ConsoleKeyInfo keyInfo)
	{
		KeyBindings.TryGetValue(keyInfo.Key, out var action);
		if (action != null)
		{
			action.Invoke(keyInfo);
		}
	}
	
	public void PrintExpandableGroups<T>(
		IEnumerable<T> sections,
		bool expandInSeparatePanel = false) where T : IExpandableSection
	{
		for (int i = 0; i < sections.Count(); i++)
		{
			var section = sections.ElementAt(i);
			string prefix = section.IsExpandable 
				? section.IsExpanded ? "[[-]] " : "[[+]] "
				: "> ";

			var template = $"{prefix}{{0}}";
			var color = section.IsExpandable ? "[blue]" : "[yellow]";
			template = color + template + "[/]";
			if (i == CurrentSelection)
			{
				template = "[bold]" + template + "[/]";
			}
			
			PanelBuilder.AppendLine(string.Format(template, section.Header));
			

			if (section.IsExpanded && !expandInSeparatePanel)
			{
				foreach (var detail in section.Details)
				{
					PanelBuilder.AppendLine($"[grey]{detail}[/]");
				}
			}
			
		}
	}

	public Panel BuildStandardPanel()
	{
		return new Panel(PanelBuilder.ToString())
			.Border(PanelData.IsActive ? BoxBorder.Double : BoxBorder.Rounded)
			.BorderColor(PanelData.IsActive ? Color.Green : Color.Grey)
			.Header(PanelData.IsActive ? $"[b][green]{Header}[/][/]" : Header)
			.Padding(1, 1, 1, 1);
	}
	
	protected void OnSelectionUp(ConsoleKeyInfo keyInfo)
	{
		CurrentSelection = Math.Max(0, CurrentSelection - 1);
	}

	protected void OnSelectionDown(ConsoleKeyInfo keyInfo)
	{
		CurrentSelection = Math.Min(PanelData.ExpandableSections.Count() - 1, CurrentSelection + 1);
	}

	protected void OnMessageExpanded(ConsoleKeyInfo keyInfo)
	{
		var selected = PanelData.ExpandableSections.ElementAt(CurrentSelection);
		if (selected.IsExpandable)
		{
			selected.IsExpanded = !selected.IsExpanded;
		}
	}

	protected void OnTextBackspaced(ConsoleKeyInfo keyInfo)
	{
		TextInput = TextInput.Substring(0, TextInput.Length - 1);
	}
}