using System;
using Godot;

namespace Terrarium_Virtuel.scripts.simulation;

[GlobalClass]
public partial class GameLoop : Node
{
	public static GameLoop Instance { get; private set;}
	
	private int _ticksPerSecond = 1; // TPS setting
	private double _timeSinceLastTick; // counter
	private double _timeThreshold; 
	private bool _gamePaused = true;
	
	
	[Export]
	public long TicksPassed {get; private set;}
	
	// simpler getter for if the simulation is paused
	public bool IsPaused() => _gamePaused;
	
	// game speed property
	public int TicksPerSecond
	{
		get => _ticksPerSecond;
		set
		{
			_ticksPerSecond = Mathf.Clamp(value, 0, 1000);
			_timeThreshold = _ticksPerSecond > 0 ? 1.0 / _ticksPerSecond : Double.MaxValue;

			// safety reset
			_timeSinceLastTick = 0; 
			
			string thresholdDisplay = _ticksPerSecond > 0 ? $"{_timeThreshold:F4}s" : "INFINITY";
			GD.Print($"[GameLoop] Ticks: {_ticksPerSecond} | Threshold: {thresholdDisplay}");
		}
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Gets the start time
		ulong startTime = Time.GetTicksMsec();
		
		Instance = this;
		ConfigureGameLoop();

		// Calculates the delta
		ulong duration = Time.GetTicksMsec() - startTime;

		GD.Print($"[GameLoop] Created GameLoop in {duration}ms!!");
	}

	private void ConfigureGameLoop()
	{
		_timeSinceLastTick = 0;
		TicksPerSecond = _ticksPerSecond;
		Controller.Instance.Subscribe<SimulationSpeedSignal>(OnSimulationSpeed);
		
		// Forces the first heartbeat
		EmitHeartbeat();
	}

	public override void _ExitTree()
	{
		// removes reference when exiting. Only useful if we ever make a second game loop, for any reasons
		Controller.Instance.Unsubscribe<SimulationSpeedSignal>(OnSimulationSpeed);
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ResolveProcess(delta);
	}

	/// <summary>
	/// The companion method to _Process to clean up the source of the tick updates
	/// </summary>
	/// <param name="delta"></param>
	private void ResolveProcess(double delta)
	{
		if (!_gamePaused)
		{
			_timeSinceLastTick += delta;

			if (_timeSinceLastTick >= _timeThreshold)
			{
				ResolveUpdate(); // calls the actual method resolving the ticks
				
				_timeSinceLastTick -= _timeThreshold; // preserves leftover delta
			}
		}
	}
	
	/// <summary>
	/// The method called whenever an update actually happens. Every component of an update call comes here.
	/// </summary>
	private void ResolveUpdate()
	{
		TicksPassed++; // increments each update, starting at 1 for the first tick ever. Hence the ++ at the very beginning
		
		// [OPTIONAL] Mesures the time it takes to resolve each update. For performance analysis, if we want any
		// Comment out the lines with "Comment this" to remove them if you want to
		// ulong startTime = Time.GetTicksMsec(); // comment this 
		
		
		
		// GD.Print($"[GameLoop] Resolving update! Put the tick components here."); // <--- shit goes here 
		Controller.Instance.Emit(new SchedulerSignal()); // calls the pass_tick


		
		EmitHeartbeat(); // sends the SimulationStateSignal to all listeners, mostly the interface so it can display generic info like speed and ticks passed
		
		// Calculates the delta
		// ulong duration = Time.GetTicksMsec() - startTime; // comment this
		// GD.Print($"[GameLoop === Performance] Resolved the tick in {duration}ms!!"); // comment this
	}

	
	
	private void EmitHeartbeat()
	{
		// creates the signal that is sent manually
		var state = new SimulationStateSignal(
			isPaused: _gamePaused,
			simulationSpeed: _ticksPerSecond,
			ticksPassed: TicksPassed
		);
		
		Controller.Instance.Emit(state);
	}
	

	public void OnSimulationSpeed(SimulationSpeedSignal simulationSpeedSignal)
	{
		// Always updated to reflect simulation state
		TicksPerSecond = simulationSpeedSignal.Speed;
		
		// Simple way of switching the 'master switch' of the _Process loop
		_gamePaused = (simulationSpeedSignal.Speed == 0);
			
		EmitHeartbeat();
	}
}
