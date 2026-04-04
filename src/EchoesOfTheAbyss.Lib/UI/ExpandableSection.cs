namespace EchoesOfTheAbyss.Lib.UI;

public class ExpandableSection<T> : IExpandableSection
{
	public T Content { get; set; }

	public string Header { get; set; }

	public List<string> Details { get; set; }

	public bool IsExpandable { get; set; } = true;
	
	public bool IsExpanded { get; set; }
}