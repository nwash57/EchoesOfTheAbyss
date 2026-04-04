using EchoesOfTheAbyss.Lib.Enums;
using EchoesOfTheAbyss.Lib.UI;

namespace EchoesOfTheAbyss.Lib.Models;

public class Message : IExpandableSection
{
	public MessagerEnum Messager { get; set; }
	
	public List<string> Thoughts { get; set; }
	
	public string Speech { get; set; }
	
	// IExpandableSection
	public bool IsExpandable { get; set; } = true;
	public bool IsExpanded { get; set; } = false;

	public string Header => Speech;

	public List<string> Details => Thoughts;
}