using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Terrarium_Virtuel.scripts.database;
using Terrarium_Virtuel.scripts.saving;
using Terrarium_Virtuel.scripts.world_generation;
using Terrarium_Virtuel.signals;
using Terrarium_Virtuel.signals.action_items;
using System.Runtime.Versioning;
using Terrarium_Virtuel.signals;

public partial class Controller : Node
{
	// The access to the controller
	public static Controller Instance { get; private set;}
	
	private static Node _mainNode; // node used when changing scenes (main menu and game interface)

	private Node _activeScene; // changes between main menu and simulation
	// private Node _dbScene;
	
	// Only references the nodes. 
	private Node Scheduler { get; set;} 
	private Node GameLoop { get; set;}

	[Export]
	public Node WorldManager { get;
		set;
	}
	
	private DatabaseViewer _dbViewer;

	// dict of EventTypes -> Listeners
	private Dictionary<Type, List<Delegate>> _subscribers = new();
	
	// dict to map GDScript Callables to their C# Action wrappers for unsubscribing
	private Dictionary<Callable, Delegate> _gdWrappers = new();
	
	
	// private static string DatabaseScenePath = "res://scenes/database/database_manager.tscn";
	private static string SimulationScenePath = "res://scenes/simulation.tscn";
	private static string MainMenuScenePath = "res://scenes/ui/main_menu.tscn";
	private static string DatabaseViewerPath = "res://scenes/database/database_viewer.tscn";
	
	
	private SaveData JsonContent { get; set; }

	public override void _Ready()
	{
		
		
		SetupCommandRouting();
		
		// _dbScene = LoadScene(DatabaseScenePath, "Database");
		//AddChild(_dbScene); // adds DB straight under controller. Does not wait for Main scene.
	}
	
	
	private Node LoadScene(string scenePath, string sceneName)
	{
		var scene = GD.Load<PackedScene>(scenePath);
		var node = scene.Instantiate();
		node.Name = sceneName;
		return node;
	}

	private void SetActiveScene(string scenePath, string sceneName)
	{
		if (_activeScene != null)
		{
			_activeScene.QueueFree();
		}
		
		_activeScene = LoadScene(scenePath, sceneName);
		
		if (_mainNode != null)
		{
			_mainNode.AddChild(_activeScene);
		}
		else
		{
			GD.PushError("[Controller] SetActiveSceneFailed: _mainNode is null.");
		}
		
	}

	/// <summary>
	/// Method called from anywhere that will handle creating a new simulation and setting it as the active scene.
	/// </summary>
	private void CreateSimulation(int seed = 12345)
	{
		if (seed == 12345)
		{
			seed = Convert.ToInt32(GD.Randi() % 99999);
		}
		GD.Print("[Controller] Transitioning to Simulation...");
		
		if (_activeScene != null)
		{
			_activeScene.QueueFree();
			_activeScene = null; 
		}
		
		SetActiveScene(SimulationScenePath, "ActiveSimulation");
		
		Simulation.Instance.SimulationConfig.Seed = seed;
		Simulation.Instance.BeginRegistration();
		
		// 4. Hook up your internal references
		Scheduler = _activeScene.GetNode("Scheduler");
		GameLoop = _activeScene.GetNode("GameLoop");
		WorldManager = _activeScene.GetNode("WorldManager");
		
		// 5. Making the WorldManager create the environment while taking into acount the Simulation Config
		WorldManager.Set("current_seed",Simulation.Instance.SimulationConfig.Seed);
		WorldManager.Call("create_environment");
		
		GD.Print("[Controller] Simulation Spawned under Main Node.");
		Emit(new NewSimulationSignal());
	}

	public Vector3I GetMapMousePos()
	{
		Vector3I vector3i = (Vector3I)WorldManager.Call("get_local_to_map");
		return vector3i;
	}

	/// <summary>
	/// Method to return to main menu, duh
	/// </summary>
	private void ReturnToMainMenu()
	{
		GD.Print("[Controller] Exiting Simulation. Cleaning up systems...");

		Scheduler = null;
		GameLoop = null;
		
		SetActiveScene("res://scenes/ui/main_menu.tscn", "MainMenu");
	
		// GD.Print("[Controller] Successfully returned to Main Menu.");
	}
	


	/// <summary>
	/// A method that is called by the `Main` node in the `Main` scene.
	/// It is used only so the controller can spawn scenes under it, mainly the MainMenu and Simulation scene.
	/// </summary>
	/// <param name="main"></param>
	public void RegisterMainNode(Node main)
	{
		_mainNode = main;
		// GD.Print("[Controller] Main Node registered. Initializing Menu...");
		_mainNode.AddChild(LoadScene(DatabaseViewerPath, "DatabaseViewer"));
		SetActiveScene(MainMenuScenePath, "MainMenu");
	}

	// Override method that guarrantees that the controller script is loaded first, before everything else.
	public override void _EnterTree()
	{
		// GD.Print($"[Controller] Master Script is Loading...");
		if (Instance == null)
		{
			GD.Print("[Controller] No Pre-existing Controller Instance found, creating one...");
			Instance = this;
		}
		else
		{
			GD.Print("[Controller] Pre-existing Controller Instance found, overwriting old one...");
			QueueFree();
		}
	}

	/// <summary>
	/// The method called to add a subscriber to listen for a certain actionEven signal
	/// </summary>
	/// <param name="listener"></param>
	/// <typeparam name="T"></typeparam>
	public void Subscribe<T>(Action<T> listener)
	{
		var type = typeof(T);
		if (!_subscribers.ContainsKey(type))
		{
			_subscribers[type] = new List<Delegate>();
		}
		_subscribers[type].Add(listener);
	}

	/// <summary>
	/// The method called to remove a subscriber from listening for a certain actionEvent signal
	/// </summary>
	/// <param name="listener"></param>
	/// <typeparam name="T"></typeparam>
	public void Unsubscribe<T>(Action<T> listener)
	{
		var type = typeof(T);
		if (_subscribers.ContainsKey(type))
		{
			_subscribers[type].Remove(listener);
		}
	}

	/// <summary>
	/// The one method to signal them all...
	/// It uses a dict of types: list to group every listener to their own channels.
	/// It will send the simulationEvent to every listener that listens for that simulationEvent.
	/// It only works with C# code, not GDScript
	/// </summary>
	/// <param name="simulationEvent"></param>
	/// <typeparam name="T"></typeparam>
	public void Emit<T>(T simulationEvent)
	{
		var type = typeof(T);
		
		//GD.Print($"[Controller] Emitting {type.Name}");
		
		if (!_subscribers.TryGetValue(type, out var listeners))
		{
			// emits nothing because there is no list of listeners
			// DO NOT REFORMAT
			return;
		}
		else
		{
			// iterates over a copy for ConcurrentException management
			foreach (var listener in new List<Delegate>(listeners))
			{
				// automatically prunes dead references from list
				if (listener.Target is GodotObject obj && !GodotObject.IsInstanceValid(obj))
				{
					GD.Print("[Controller] Pruning C# dead reference...");
					listeners.Remove(listener);
					continue;
				}
				
				((Action<T>)listener)?.Invoke(simulationEvent);
			}
		}
	}

	// ==========================================
	// GDSCRIPT BRIDGE METHODS
	// ==========================================

	/// <summary>
	/// Method called in order to subscribe to a signal whenever it is emitted.
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="listener"></param>
	public void GDSubscribe(string eventName, Callable listener)
	{
		Type type = Type.GetType(eventName);

		// Why feed it an empty string, dude?
		if (type == null)
		{
			GD.PrintErr($"[Controller] GDSubscribe Failed: Could not find Event Type '{eventName}'. Ensure it exists in C#.");
			return;
		}

		// checks if it already has a list of that signal type.
		if (!_subscribers.ContainsKey(type))
		{
			_subscribers[type] = new List<Delegate>();
		}

		// Wrap the GDScript Callable in a C# Action
		// This is only executed when emitted, not during this method's calls.
		Action<SimulationSignal> wrapper = (ev) =>
		{
			var target = listener.Target as GodotObject;

			if (GodotObject.IsInstanceValid(target))
			{
				listener.Call(ev);
			}
			else
			{
				GD.Print("[Controller] Tried to reference a dead node.");
			}
		};
		
		_gdWrappers[listener] = wrapper;
		_subscribers[type].Add(wrapper);
	}
	
	/// <summary>
	/// GDScript unsubscribes using the string name of the event and their function.
	/// Usage in GDScript: Controller.gd_unsubscribe("PauseGameEvent", _on_pause)
	/// </summary>
	public void GDUnsubscribe(string eventName, Callable listener)
	{
		Type type = Type.GetType(eventName);
		
		if (type == null) return;
		
		if (_gdWrappers.TryGetValue(listener, out var wrapper))
		{
			if (_subscribers.ContainsKey(type))
			{
				_subscribers[type].Remove(wrapper);
			}
			
			_gdWrappers.Remove(listener);
		}
	}
	
	/// <summary>
	/// The GDScript implementation of the controller signal bus.
	/// </summary>
	/// <param name="event"></param>
	public void GDEmit(SimulationSignal @event)
	{
		if (@event == null)
		{
			GD.PrintErr($"[Controller] GDEmit: Null SimulationEvent");
			return;
		}

		var type = @event.GetType();
		// GD.Print($"[Controller] GD-Emitting {type.Name}");

		if (_subscribers.TryGetValue(type, out var listeners))
		{
			// use a copy to avoid concurrent modif errors
			var listenersCopy = new List<Delegate>(listeners);
			foreach (var listener in listenersCopy)
			{
				if (listener.Target is GodotObject obj && !GodotObject.IsInstanceValid(obj))
				{
					listeners.Remove(listener); // removes if there is no longer a reference
					continue; // skips the rest of the loop
				}
				
				listener.DynamicInvoke(@event);
			}
		}
	}
	
	
	/// <summary>
	/// Method that sets up the command routing.
	/// The `command.Payload` must match the string given to the UISignal constructor.
	/// </summary>
	private void SetupCommandRouting()
	{
		Subscribe<UISignal>(payload => 
		{
			switch (payload.Command)
			{
				case "OnPauseGame":
					// This converts the "String" into the "Object" the GameLoop wants
					// Emit(new SimulationSpeedSignal()); 
					GD.Print("[Controller] OnPauseGame should not be called. deprecated method.");
					break;

				case "OnIncreaseSpeed":
					// You could emit a SpeedChangeSignal here later
					break;

				case "OnCreateSimulation":
					CreateSimulation();
					break;

				case "OnConfirmExit":
					// This is called AFTER the user clicks "Yes" in the popup
					ReturnToMainMenu(); // private method since only every called from inside.
					break;

				case "OnReturnToMainMenu":
					Emit(new ConfirmDialogSignal(
						"Êtes-vous sûr de quitter ? Tout progrès non sauvegardé sera perdu."));
					// ReturnToMainMenu(); // private method
					break;
				
				case "OnSaveGame":
					Emit(new SaveSimulationSignal());
					break;

				case "OnDBRandomEntity":
					var data = DatabaseManager.Instance.GetPopulationInRange(Simulation.Instance.SimulationId, 0, 200);

					foreach (var entity in data)
					{
						GD.Print($"[GetPopulationInRange] Tick ({entity.Tick}), Count: {entity.Count}, SpeciesId: {entity.SpeciesId}");
					}

					break;
			
				case "OpenDBWindow":
					DatabaseViewer.Instance.Mode = Window.ModeEnum.Windowed;
					break;
			}
			
			
		});
	}


	public void LoadSimulation(string filePath)
	{
		JsonContent = LoadingJson.LoadJsonSave(filePath);
		
		CreateSimulation(JsonContent.Seed);
		Subscribe<PathfindBakingCompleteSignal>(FinalizeLoading);
	}

	public void FinalizeLoading(PathfindBakingCompleteSignal signal)
	{
		foreach (var critter in JsonContent.Entities)
		{
			var str = critter.Position.Trim().Replace("(", "").Replace(")", "");
			var pos = str.Split(',');
			
			var posI = new Vector3I(int.Parse(pos[0]), int.Parse(pos[1]), int.Parse(pos[2]));
			
			if (int.TryParse(critter.Species, out int id))
			{
				var species = ActionShelf.Instance.CategoryItems["Espèces"];
				
				
				foreach (var specie in species)
				{
					if (specie.ID == id.ToString())
					{
						specie.ExecuteAction(posI);
					}
				}
			}
			
			if (critter.Category == "Nourriture")
			{
				foreach (var food in ActionShelf.Instance.CategoryItems["Nourriture"])
				{
					if (food.Label == critter.Species)
						food.ExecuteAction(posI);
				}
			}
		}
		Unsubscribe<PathfindBakingCompleteSignal>(FinalizeLoading);
	}

	
}
