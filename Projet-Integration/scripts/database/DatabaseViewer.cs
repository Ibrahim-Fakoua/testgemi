using Godot;
using System;
using System.Collections.Generic;
using Terrarium_Virtuel.scripts.database;
using Terrarium_Virtuel.signals.database;

public partial class DatabaseViewer : Window
{
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
		SimButton = GetNode<OptionButton>("%SimButton");
		SimButton.AllowReselect = true;
		
		GraphTabContainer = GetNode<TabContainer>("%GraphTabContainer");
		
		GraphTabContainer.AddChild(_populationGraph = new PopulationGraph());
		
		
		
		DefineSignals();
		FetchSimulations();
	}

	private void DefineSignals()
	{
		SimButton.ItemSelected += (index) =>
		{
			_simId = _simulations[(int)index].SimId;
			GD.Print("[DatabaseViewer] Selected Simulation #" + _simId);
			RefreshGraphs();
		};
		
		//Controller.Instance.Subscribe<UpdateGraphSignal>(NewSqlData);
		Controller.Instance.Subscribe<SimulationRegisteredSignal>(NewSimulationStarted);
	
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			Mode = ModeEnum.Minimized;
		}
	}

	/**
	public void NewSqlData(UpdateGraphSignal signal)
	{
		GD.Print("Refreshing Graphs...");
		RefreshGraphs();
	}
	*/

	public void NewSimulationStarted(SimulationRegisteredSignal signal)
	{
		GD.Print("Refreshing Simulations...");
		FetchSimulations();
	}

	private void FetchSimulations()
	{
		var simus = DatabaseManager.Instance.GetAllSimulations();
		
		if (simus.Count == 0 || _simulations.Count == simus.Count)
		{
			return;
		}
		GD.Print($"[FetchingSimulations] DB: {simus.Count}, Local: {_simulations.Count}");
		foreach (var sim in simus)
		{
			
			if (!_simIdDict.ContainsKey(sim.SimId))
			{
				var label = $"Sim #{sim.SimId} — Seed {sim.SimSeed}";
				GD.Print(label);
				_simIdDict[sim.SimId] = sim;
				SimButton.AddItem(label);
			}
		}
		_simulations = simus;
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
        
		foreach (var sim in _simulations)
		{
			var label = $"Sim #{sim.SimId} — Seed {sim.SimSeed}";
			SimButton.AddItem(label);
		}

		if (_simulations.Count > 0)
		{
			_simId = _simulations[0].SimId;
			RefreshGraphs();
		}
	}

}
