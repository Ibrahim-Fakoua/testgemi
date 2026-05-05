using Godot;

[GlobalClass]
public partial class NewSpeciesSignal : SimulationSignal
{
	public string Id { get; set; }
	public string Label { get; set; }
	public string IconPath { get; set; }

	public NewSpeciesSignal(string id, string label, string iconPath)
	{
		Id = id;
		Label = label;
		IconPath = iconPath;
	}
}
