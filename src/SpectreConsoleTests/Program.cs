using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading;


 
var collapsibleSections = new List<(string Title, string Content, bool IsExpanded)>
{
    ("Section 1", "This is the content for section 1.\nIt can span multiple lines.", false),
    ("Section 2", "Content for section 2.\nMore details here.", false),
    ("Section 3", "Section 3 information goes here.\nLots of useful data.", false)
};

int currentSelection = 0;
bool running = true;

while (running)
{
    Console.Clear();
    AnsiConsole.Write(new Rule("[yellow]Collapsible Sections Demo[/]").RuleStyle("grey").DoubleBorder());
    
    // Display instructions
    AnsiConsole.MarkupLine("[grey]Use [blue]↑/↓[/] to navigate, [green]SPACE[/] to expand/collapse, [red]ESC[/] to exit[/]");
    AnsiConsole.WriteLine();

    // Display sections
    for (int i = 0; i < collapsibleSections.Count; i++)
    {
        var (title, content, isExpanded) = collapsibleSections[i];
        string prefix = isExpanded ? "[[-]] " : "[[+]] ";
        
        // Highlight selected section
        if (i == currentSelection)
        {
            AnsiConsole.MarkupLine($"[bold blue]{prefix}{title}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"{prefix}{title}");
        }

        // Show content if expanded
        if (isExpanded)
        {
            var panel = new Panel(content)
                .Border(BoxBorder.Rounded)
                .Collapse()
                .Padding(1, 1, 1, 1);
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
    }

    // Handle keyboard input
    var key = Console.ReadKey(true).Key;
    switch (key)
    {
        case ConsoleKey.UpArrow:
            currentSelection = Math.Max(0, currentSelection - 1);
            break;
        case ConsoleKey.DownArrow:
            currentSelection = Math.Min(collapsibleSections.Count - 1, currentSelection + 1);
            break;
        case ConsoleKey.Spacebar:
            // Toggle expansion state
            var section = collapsibleSections[currentSelection];
            collapsibleSections[currentSelection] = (section.Title, section.Content, !section.IsExpanded);
            break;
        case ConsoleKey.Escape:
            running = false;
            break;
    }
}