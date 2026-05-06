using Godot;
using System;
using System.Collections.Generic;
using Terrarium_Virtuel.scripts.database;
using Terrarium_Virtuel.signals.database;

public partial class DatabaseViewer : Window
{
	public static DatabaseViewer Instance;
	
	private DatabaseManager _dbManager = DatabaseManager.Instance;
	
	private List<SimulationRow> _simulations = new ();
	
	private Dictionary<string, IGraph> GraphDict = new ();
	
	private PopulationGraph _populationGraph;
	
	private OptionButton SimButton { set; get; }
	private long _simId = -1;
	private Dictionary<int, SimulationRow>  _simIdDict = new();
	
	private double TimeCounter = 0;
	
	private TabContainer GraphTabContainer { set; get; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		SimButton = GetNode<OptionButton>("%SimButton");
		SimButton.AllowReselect = true;
		
		GraphTabContainer = GetNode<TabContainer>("%GraphTabContainer");
		
		GraphTabContainer.AddChild(_populationGraph = new PopulationGraph());
		
		
		
		DefineSignals();
		PopulateSimulationSelector();
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
		// GD.Print("NEW DATA BAYBEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
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
	}
	
	private void AddGraph<T>(string name, GraphClasses<T> graph) where T : DataQuery
	{
		GraphDict.Add(name, graph);
		GraphTabContainer.AddChild(graph);
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

}
