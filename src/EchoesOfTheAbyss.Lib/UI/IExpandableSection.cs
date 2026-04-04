namespace EchoesOfTheAbyss.Lib.UI;

public interface IExpandableSection
{
	public string Header { get; }
	public List<string> Details { get; }
	public bool IsExpandable { get; }
	public bool IsExpanded { get; set; }
}