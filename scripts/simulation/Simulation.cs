using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot.Collections;
using Microsoft.VisualBasic;
using Terrarium_Virtuel.scripts.database;
using Terrarium_Virtuel.scripts.saving;
using Terrarium_Virtuel.scripts.simulation;
using Terrarium_Virtuel.signals;
using Terrarium_Virtuel.signals.database;

[GlobalClass]
public partial class Simulation : Node
{
	public static Simulation Instance { get; private set; }

	[Export] public int SimulationId { get; private set; }

	[Export] public string SaveDirectory = "res://_persistent/saves";

	public SimConfig SimulationConfig { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		SimulationConfig = new SimConfig(12345);
		Controller.Instance.Subscribe<SaveSimulationSignal>(SaveSimulation);
	}

	public void BeginRegistration()
	{
		GD.Print("Begin Registration");
		Callable callback = new Callable(this, nameof(RegisteredSimulation));
		DatabaseManager.Instance.RegisterSimulation(callback, SimulationConfig);
	}

	public void RegisteredSimulation(int id)
	{
		SimulationId = id;
		GD.Print("Registration completed, ID : " + SimulationId);
		DatabaseManager.Instance.SeedBaseSpecies(id);
	}

	/// <summary>
	/// This method is the intemediary between the gameInterface scene which has the file dialog as a node
	/// It is called from the controller, because funky signals lmao.
	/// </summary>
	/// <param name="signal"></param>
	public void SaveSimulation(SaveSimulationSignal signal)
	{
		var gameInterface = GetNode<GameInterface>("GameInterface");

		// Give it a method as an Action<string, string> so it can call it once the dialog is confirmed.
		gameInterface.OpenSaveNameDialog(SaveSimulationToDisk, SimulationId, SaveDirectory);
	}

	public void SaveSimulationToDisk(string directory, string fileName)
	{
		var worldManager = Controller.Instance.WorldManager as Node;
		Node mainLayer = null;
		foreach (Node child in worldManager.GetChildren())
		{
			if ((String) child.Get("name") == "MainLayer") 
			{
				mainLayer = child;
			}
		}	
		
		var globalDirectory = ProjectSettings.GlobalizePath(directory);
		
		var path = Path.Combine(globalDirectory, fileName);
		
		var saveData = new SaveData
		{
			Seed = SimulationConfig.Seed,
			Tick = (int)GameLoop.Instance.TicksPassed,
		};
		
		if (mainLayer == null) return;

		foreach (Node child in mainLayer.GetChildren())
		{
			var vString = ((Vector3I)child.Get("position")).ToString();
			var type = (string)child.Get("type");
			string toStore = null;
			string category = null;
			
			
			switch (type)
			{
				case "Carrot":
					toStore = "Carotte";
					category = "Nourriture";
					break;
				
				case "ApplePile":
					toStore = "Pommes";
					category = "Nourriture";
					break;
				
				case "AppleTree":
					toStore = "Pommier";
					category = "Nourriture";
					break;
				case "BerryBush":
					toStore = "Buisson à baies";
					category = "Nourriture";
					break;

				case "Critter":
					toStore = (String)child.Get("specie"); // match l'ID des action items.
					category = "Espèces";
					break;
			}
			
			if (toStore != null)
			{
				var entity = new Entity
				{
					Position = vString,
					Species = toStore,
					Category = category
				};
			
				saveData.Entities.Add(entity);
			}
			

		}

		var options = new JsonSerializerOptions { WriteIndented = true };
		String jsonString = JsonSerializer.Serialize(saveData, options);
		
		File.WriteAllText(path, jsonString);
	}

	public void OnLoadedFromSave()
	{
		Controller.Instance.Emit(new SimulationRegisteredSignal());
	}
}

/// <summary>
/// Honestly, this might be pretty useless, not gonna lie...
/// </summary>
public class SimConfig
{
	
	[JsonPropertyName("seed")]
	public int Seed { get; set; }


	public SimConfig(int seed)
	{
		Seed = seed;
	}
}
