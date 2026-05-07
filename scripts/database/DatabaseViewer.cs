using Godot;
using System;
using System.Collections.Generic;
using Terrarium_Virtuel.scripts.database;
using Terrarium_Virtuel.signals.database;

public partial class DatabaseViewer : Window
{
	private DatabaseManager _dbManager = DatabaseManager.Instance;
	
	private ViewerToolBarMenu _menuBar;
	
	private List<SimulationRow> _simulations = new ();
	
	private Dictionary<string, IGraph> GraphDict = new ();
	
	private PopulationGraph _populationGraph;
	private SpeciesActionTab _speciesActionTab;
	
	private OptionButton SimButton { set; get; }
	private long _simId = -1;
	private Dictionary<int, SimulationRow>  _simIdDict = new();
	
	private double TimeCounter = 0;

	private double _tickIntervals = 400; // in ms
	
	private bool IsAutoRefresh = false;
	
	private TabContainer GraphTabContainer { set; get; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SimButton = GetNode<OptionButton>("%SimButton");
		SimButton.AllowReselect = true;
		
		GraphTabContainer = GetNode<TabContainer>("%GraphTabContainer");
		_menuBar = GetNode<ViewerToolBarMenu>("%ViewerMenuBar");
		_menuBar.InitializeFromConfig("res://config/ui/viewer_toolbar_config.json");
		
		
		GraphTabContainer.AddChild(_populationGraph = new PopulationGraph());
		GraphTabContainer.AddChild(_speciesActionTab = new SpeciesActionTab());
		
		DefineSignals();
		PopulateSimulationSelector();
		SetupRouting();
	}

	private void DefineSignals()
	{
		SimButton.ItemSelected += (index) =>
		{
			_simId = _simulations[(int)index].SimId;
			// GD.Print("[DatabaseViewer] Selected Simulation #" + _simId);
			RefreshGraphs();
		};
		
		
		Controller.Instance.Subscribe<UpdateGraphSignal>(NewSqlData);
		Controller.Instance.Subscribe<SimulationRegisteredSignal>(NewSimulationStarted);
	
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			Mode = ModeEnum.Minimized;
		}
	}

	public void NewSqlData(UpdateGraphSignal signal)
	{
		if (Mode == ModeEnum.Minimized) return;
		if (!IsAutoRefresh) return;
		RefreshGraphs();
	}

	public void NewSimulationStarted(SimulationRegisteredSignal signal)
	{
		// GD.Print("Refreshing Simulations...");
		PopulateSimulationSelector();
	}

	private void RefreshGraphs()
	{
		if (_simId == -1) return;
		var (minTick, maxTick) = DatabaseManager.Instance.GetTickRange((int)_simId);
		var data = DatabaseManager.Instance.GetPopulationInRange((int)_simId, minTick, maxTick);
		_populationGraph.SetData(data);
		_speciesActionTab.SetSimulation((int)_simId);
	}
	
	private void AddGraph<T>(string name, DataGraph<T> dataGraph) where T : DataQuery
	{
		GraphDict.Add(name, dataGraph);
		GraphTabContainer.AddChild(dataGraph);
	}
	
	private void PopulateSimulationSelector()
	{
		_simulations = DatabaseManager.Instance.GetAllSimulations();
		SimButton.Clear();
		_simIdDict.Clear();

		foreach (var sim in _simulations)
		{
			var label = $"Sim #{sim.SimId} — Seed {sim.SimSeed}";
			_simIdDict[sim.SimId] = sim;
			SimButton.AddItem(label);
		}

		if (_simulations.Count > 0)
		{
			_simId = _simulations[0].SimId;
			RefreshGraphs();
		}
	}

	private void SetupRouting()
	{
		Controller.Instance.Subscribe<DBViewerSignal> (payload =>
		{
			switch (payload.Command)
			{
				case "OnRefresh":
					RefreshGraphs();
					break;
				
				case "OnEmptyDatabase":
					DatabaseManager.Instance.EmptyDatabase();
					PopulateSimulationSelector();
					break;
				case "OnAutoRefresh":
					IsAutoRefresh = !IsAutoRefresh;
					break;
				case "OnShowDeaths":
					_populationGraph.ShowDeathMarkers = !_populationGraph.ShowDeathMarkers;
					RefreshGraphs();
					break;
				case "OnShowBirths":
					RefreshGraphs();
					_populationGraph.ShowBirthMarkers = !_populationGraph.ShowBirthMarkers;
					break;
			}
		});
	}

}
