using Godot;
using System;
using Terrarium_Virtuel.signals;

// Using aliases to avoid namespace conflicts
using GDictionary = Godot.Collections.Dictionary;
using GArray = Godot.Collections.Array;
public partial class MainMenu : Node
{

	private Button _newSimulationButton;
	private Button _loadSimulationButton;
	private Button _openSettingsButton;
	private Button _quitButton;
	
	private AcceptDialog _newSimulationDialog;
	private FileDialog _loadSimulationDialog;
	
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_newSimulationButton = GetNode<Button>("%NewSimulationButton");
		_loadSimulationButton = GetNode<Button>("%LoadSimulationButton");
		_quitButton = GetNode<Button>("%QuitButton");
		
		_newSimulationDialog = GetNode<AcceptDialog>("%NewSimulationDialog");
		_loadSimulationDialog = GetNode<FileDialog>("%LoadSimulationDialog");
		
		_newSimulationButton.Pressed += NewSimulation;
		_loadSimulationButton.Pressed += LoadSimulation;
		_quitButton.Pressed += QuitGame;
		
		_loadSimulationDialog.FileSelected += (path) => {
			Controller.Instance.LoadSimulation(path);
		};
	}

	
	public override void _ExitTree()
	{
		// ... empty shit
	}
	
	/// <summary>
	/// Placeholder method that would call 
	/// </summary>
	private void LoadSimulation()
	{
		_loadSimulationDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_loadSimulationDialog.Access = FileDialog.AccessEnum.Resources;
		_loadSimulationDialog.Filters = [ "*.json ; Scene Files" ];
		_loadSimulationDialog.CurrentDir = "res://_persistent/saves";
		_loadSimulationDialog.PopupCentered();

	}

	private void LoadSimulationFromDisk(string path)
	{
		Controller.Instance.LoadSimulation(path);
		GD.Print("Simulation Loaded.");
	}

	private void OpenSettings()
	{
		// opens the settings dialog window.
		// I just attached the old script to the SettingsDialog but need to confirm if it works.
	}
	
	private void NewSimulation()
	{
		Controller.Instance.Emit(new UISignal("OnCreateSimulation"));
	}

	private void QuitGame()
	{
		// safely closes any SQL connection or sensitive files before forcefully closing
		Controller.Instance.Emit(new GracefulStopSignal());
		
		
		// closes the game
		GetTree().Quit();
	}
}
