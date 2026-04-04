namespace EchoesOfTheAbyss.Lib.UI;

public class PanelData
{
	public bool IsActive { get; set; }
	public IEnumerable<IExpandableSection> ExpandableSections { get; set; }
	public string TextInput { get; set; }
}