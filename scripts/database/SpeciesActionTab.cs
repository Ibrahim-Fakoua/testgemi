using Godot;
using System.Collections.Generic;
using Terrarium_Virtuel.scripts.database;

public partial class SpeciesActionTab : Control
{
    private OptionButton _speciesSelector;
    private SpeciesActionPieChart _pieChart;
    
    private int _simId = -1;
    private List<(int DbId, int EnumIndex)> _speciesList = new();

    public override void _Ready()
    {
        Name = "Actions des espèces";
        
        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(vbox);

        _speciesSelector = new OptionButton();
        vbox.AddChild(_speciesSelector);

        _pieChart = new SpeciesActionPieChart();
        _pieChart.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(_pieChart);

        _speciesSelector.ItemSelected += OnSpeciesSelected;
    }

    public void SetSimulation(int simId)
    {
        _simId = simId;
        PopulateSpeciesSelector();
    }

    private void PopulateSpeciesSelector()
    {
        _speciesSelector.Clear();
        _speciesList.Clear();

        var species = DatabaseManager.Instance.GetSpeciesForSim(_simId);
        foreach (var s in species)
        {
            _speciesSelector.AddItem($"Species #{s.SpeId}");
            _speciesList.Add((s.SpeId, 0));
        }

        if (_speciesList.Count > 0)
            RefreshChart(0);
    }

    private void OnSpeciesSelected(long index)
    {
        RefreshChart((int)index);
    }

    private void RefreshChart(int index)
    {
        if (_simId == -1 || index >= _speciesList.Count) return;
        var data = DatabaseManager.Instance.GetActionCountsBySpecies(_simId, _speciesList[index].DbId);
        _pieChart.SetData(data);
    }
}