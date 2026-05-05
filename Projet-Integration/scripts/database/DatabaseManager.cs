using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Godot;
using Terrarium_Virtuel.signals;
using Terrarium_Virtuel.signals.database;

namespace Terrarium_Virtuel.scripts.database;

/// <summary>
/// A sort of enum-like hack to get type-safe strings. 
/// </summary>


/// <summary>
/// This is the holy grail of this project (at least, to me it is...)
/// Basically, it manages the entire SQL database for the simulation.
/// This one is basically just the relay between the signals received by the controller and
/// the Database class, which is the actual entrypoint to the SQL Database.
/// </summary>
public partial class DatabaseManager : Node
{
	

	public static DatabaseManager Instance { get; private set;}
	private static string DbFileName = "db.sqlite";
	private static string DatabaseResourcePath = "res://_persistent/db.sqlite";
	
	
	private Database _database;
	private Thread _thread;
	private CancellationTokenSource _cts;
	
	public Dictionary<int, int> SpeciesIdMap { get; private set; } = new(); // enum value → DB id
	private Queue<int> _pendingSpeciesIndices = new();
	
	
	private int _speciesSeeded = 0;
	private int _totalSpeciesToSeed = 11;

	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		_database = new Database();
		_database.GlobalDatabasePath = ProjectSettings.GlobalizePath(DatabaseResourcePath);
		
		_database.InitializeDatabase();
		
		_cts = new CancellationTokenSource();
		
		_thread = new Thread(() => _database.LoopThroughQueue(_cts.Token))
		{
			IsBackground = true,
			Name = "SQL_Worker_Thread",
		};
		_thread.Start();
		
		DefineSignals();
	}

	private void DefineSignals()
	{
		// ... here goes the subscription to the signals
		Controller.Instance.Subscribe<GracefulStopSignal>(GracefulShutdown);
		// Controller.Instance.Subscribe<NewSimulationSignal>(OnNewSimulationStarted);

	}

	public override void _ExitTree()
	{
		// ... here goes the unsubscriptions
		Controller.Instance.Unsubscribe<GracefulStopSignal>(GracefulShutdown);
		// Controller.Instance.Unsubscribe<NewSimulationSignal>(OnNewSimulationStarted);
	}

	public void GracefulShutdown(GracefulStopSignal signal)
	{
		_cts.Cancel();
		_thread.Join();
	}

	public void RegisterSpecies(Callable returnMethod, int parentSpeciesId, int birthTick)
	{
		var speEntry = new CreationQueries.SpeciesEntry 
		{
			SpeParentId = parentSpeciesId,
			SpeBirthTick = birthTick,
			SpeSimId = Simulation.Instance.SimulationId 
		};
		_database.Save(speEntry, returnMethod);
	}

	public void RegisterCreature(Callable returnMethod, int parentCreatureId, int birthTick, int speciesId)
	{
		var creEntry = new CreationQueries.CreatureEntry
		{
			CreParentId = parentCreatureId,
			CreBirthTick = birthTick,
			CreSpeId = speciesId,
			CreSimId = Simulation.Instance.SimulationId,
			CreSpeFounder = parentCreatureId == 0 // Or however you want to pass the IsFounder flag
		};
		_database.Save(creEntry, returnMethod);
		// Controller.Instance.Emit(new UpdateGraphSignal());
	}
	
	public void RegisterCreatureDeath(int creatureId, int deathTick)
	{
		var entry = new CreationQueries.CreatureEntry { CreId = creatureId, CreDeathTick = deathTick };
		_database.Update(entry);
		// Controller.Instance.Emit(new UpdateGraphSignal());
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="returnMethod"></param>
	/// <param name="simSeed"></param>
	/// <param name="simConfig"></param> The JSON string of the simulation config.
	public void RegisterSimulation(Callable returnMethod, SimConfig simConfig)
	{
		var simEntry = new CreationQueries.SimulationEntry
		{
			SimSeed = simConfig.Seed,
			SimStartTime = (int)Time.GetUnixTimeFromSystem(),
			SimConfig = JsonSerializer.Serialize(simConfig) // Or however you serialize your config
		};
		
		_database.Save(simEntry, returnMethod);
		// Controller.Instance.Emit(new UpdateGraphSignal());
	}
	
	
	public void RegisterLogAction(int actorId, string logAction, string logDetails, int logTick)
	{
		GD.Print($"[DatabaseManager] Registering log action: ID ({actorId}), Action ({logAction}), Details ({logDetails}), Tick ({logTick}), Sim ID ({Simulation.Instance.SimulationId})");
		var logEntry = new CreationQueries.LogEntry
		{
			LogActorId = actorId,
			LogAction = logAction,
			LogDetails = logDetails,
			LogTick = logTick,
			LogSimId = Simulation.Instance.SimulationId
		};
		_database.Save(logEntry);
		// Controller.Instance.Emit(new UpdateGraphSignal());
	}
	
	public void SeedBaseSpecies(int simId)
	{
		_speciesSeeded = 0;
	
		_pendingSpeciesIndices.Clear();
		
		for (int i = 0; i < _totalSpeciesToSeed; i++)
		{
			_pendingSpeciesIndices.Enqueue(i);
			var callback = new Callable(this, nameof(_onBaseSpeciesRegistered));
			RegisterSpecies(callback, 0, 0);
		}
	}
	
	public void _onBaseSpeciesRegistered(int dbId)
	{
		int enumIndex = _pendingSpeciesIndices.Dequeue();
		SpeciesIdMap[enumIndex] = dbId;
		_speciesSeeded++;
		// GD.Print($"[DatabaseManager] Species seeded: {_speciesSeeded}/{_totalSpeciesToSeed}, index={enumIndex}, dbId={dbId}");

		if (_speciesSeeded == _totalSpeciesToSeed)
		{
			// GD.Print("[DatabaseManager] All base species seeded.");
			Controller.Instance.Emit(new SimulationRegisteredSignal(Simulation.Instance.SimulationId));
		}
	}
	
	public int GetSpeciesDbId(int enumIndex)
	{
		if (SpeciesIdMap.TryGetValue(enumIndex, out int dbId))
			return dbId;
	
		GD.PushError($"[DatabaseManager] No DB id found for species enum index {enumIndex}");
		return -1;
	}
	
	
	

	public List<ActionCountRow> GetActionCounts(int simId)
	{
		return _database.Query<ActionCountRow>(
			"SELECT LOG_ACTION as LogAction, COUNT(*) as Count FROM Logs WHERE LOG_SIM_ID = @SimId GROUP BY LOG_ACTION",
			new { SimId = simId }
		).ToList();
	}
	
	

	public List<PopulationAtTickRow> GetPopulationAtTick(int simId, int tick)
	{
		return _database.Query<PopulationAtTickRow>(
			@"SELECT CRE_SPE_ID as SpeciesId, COUNT(*) as Count
					FROM Creatures
					WHERE CRE_SIM_ID = @SimId
					AND CRE_BIRTH_TICK <= @Tick
					AND (CRE_DEATH_TICK IS NULL OR CRE_DEATH_TICK > @Tick)
					GROUP BY CRE_SPE_ID",
			new { SimId = simId, Tick = tick }
		).ToList();
	}
	
	

	public List<PopulationRow> GetPopulationInRange(int simId, int fromTick, int toTick)
	{
		return _database.Query<PopulationRow>(
			@"WITH RECURSIVE ticks(tick) AS (
					SELECT @FromTick
					UNION ALL
					SELECT tick + 1 FROM ticks WHERE tick < @ToTick
				),
				alive AS (
					SELECT t.tick, c.CRE_SPE_ID,
					       COUNT(*) as Count,
					       SUM(CASE WHEN c.CRE_BIRTH_TICK = t.tick THEN 1 ELSE 0 END) as Births
					FROM ticks t
					JOIN Creatures c
					  ON  c.CRE_BIRTH_TICK <= t.tick
					  AND (c.CRE_DEATH_TICK IS NULL OR c.CRE_DEATH_TICK > t.tick)
					  AND c.CRE_SIM_ID = @SimId
					GROUP BY t.tick, c.CRE_SPE_ID
				),
				deaths AS (
					SELECT t.tick, c.CRE_SPE_ID, COUNT(*) as Deaths
					FROM ticks t
					JOIN Creatures c ON c.CRE_DEATH_TICK = t.tick AND c.CRE_SIM_ID = @SimId
					GROUP BY t.tick, c.CRE_SPE_ID
				)
				SELECT a.tick AS Tick, a.CRE_SPE_ID AS SpeciesId,
				       a.Count, a.Births,
				       COALESCE(d.Deaths, 0) AS Deaths
				FROM alive a
				LEFT JOIN deaths d ON a.tick = d.tick AND a.CRE_SPE_ID = d.CRE_SPE_ID
				ORDER BY a.tick, a.CRE_SPE_ID",
			new { SimId = simId, FromTick = fromTick, ToTick = toTick }
		).ToList();
	}
	
	

	public List<SimulationRow> GetAllSimulations()
	{
		return _database.Query<SimulationRow>(
			"SELECT SIM_ID as SimId, SIM_SEED as SimSeed, SIM_START_TIME as SimStartTime FROM Simulation ORDER BY SIM_START_TIME DESC"
		).ToList();
	}
	
	public (int min, int max) GetTickRange(int simId)
	{
		return _database.Query<(int, int)>(
			"SELECT MIN(CRE_BIRTH_TICK), MAX(CRE_BIRTH_TICK) FROM Creatures WHERE CRE_SIM_ID = @SimId",
			new { SimId = simId }
		).FirstOrDefault();
	}
}
