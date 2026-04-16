namespace EchoesOfTheAbyss.Lib.Narrative;

public class Message
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