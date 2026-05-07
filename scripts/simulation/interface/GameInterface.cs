using Godot;
using System;
using System.IO;
using Terrarium_Virtuel.signals;

public partial class GameInterface : CanvasLayer
{
	private ToolBarMenu _topBar;
	private ActionShelf _bottomBar;
	private NotificationManager _toastSystem;
	private ConfirmationDialog _dialog;

	private FileDialog _saveNameDialog;
	
	private Label _warningLabel;

	private Label _timeLabel;
	private const int TicksPerDay = 480;

	private Label _entityLabel;

	[Export] public ButtonGroup SpeedGroup;
	
	// caching the last values for small optimisations
	private long _lastTickPassed = -1;
	private bool _wasPaused = true;
	private long _lastCreatureCount;
	
	// Simulation identity
	private Simulation _simulation;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// assigns nodes to variables
		_topBar = GetNode<ToolBarMenu>("%TopMenuBar");
		_bottomBar = GetNode<ActionShelf>("%ActionTabContainer");
		_timeLabel = GetNode<Label>("%TimeCounter");

		_timeLabel.Text = "Jour 0 - 0t"; // just the time
		_timeLabel.SelfModulate = Colors.Orange;
		
		_warningLabel = GetNode<Label>("%LoadingWarningLabel");

		_dialog = GetNode<ConfirmationDialog>("%ConfirmationDialog");
		_saveNameDialog  = GetNode<FileDialog>("%SaveNameDialog");
		_dialog.Visible = false;
		_dialog.Exclusive = false;

		foreach (BaseButton button in SpeedGroup.GetButtons())
		{
			button.SetDisabled(true);
		}
		
		_simulation = GetParent<Simulation>();

		_topBar.InitializeFromConfig("res://config/ui/toolbar_config.json");
		_bottomBar.InitializeFromConfig("res://config/ui/actionshelf_config.json");
		
		DefineSignals();
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="event"></param>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventKey)
		{
			if (eventKey.ButtonIndex == MouseButton.Left && !eventKey.Pressed)
			{
				Vector3I mapCoords = Controller.Instance.GetMapMousePos();
				ActionItem item = _bottomBar.GetAndEmptyAction();

				if (item != null)
				{
					item.ExecuteAction(mapCoords);
				}
			}
		}
	}

	private void DefineSignals()
	{
		_dialog.Confirmed += OnDialogConfirmed;

		if (SpeedGroup != null)
		{
			SpeedGroup.Pressed += OnSpeedSelected;
		}
		
		Controller.Instance.Subscribe<SimulationStateSignal>(OnHeartbeatReceived);
		Controller.Instance.Subscribe<ConfirmDialogSignal>(HandleConfirmDialogSignal);
		
		Controller.Instance.Subscribe<PathfindBakingCompleteSignal>(HandlePathfindingBakingSignal);
	}

	/// <summary>
	/// Forces an unsubscribe whenever the nodes is freed.
	/// </summary>
	public override void _ExitTree()
	{
		Controller.Instance.Unsubscribe<SimulationStateSignal>(OnHeartbeatReceived);
		Controller.Instance.Unsubscribe<ConfirmDialogSignal>(HandleConfirmDialogSignal);
		Controller.Instance.Unsubscribe<PathfindBakingCompleteSignal>(HandlePathfindingBakingSignal);
	}

	/// <summary>
	/// Simply the method called when a heartbeat is sent.
	/// A heartbeat contains the status of the simulation.
	/// </summary>
	/// <param name="signal"></param>
	private void OnHeartbeatReceived(SimulationStateSignal signal)
	{
		// delegates the heartbeat to the child nodes
		_topBar.DelegateHeartbeat(signal);
		_bottomBar.DelegateHeartbeat(signal);

		// simulation is paused, nothing changed.
		if (signal.TicksPassed != _lastTickPassed || signal.IsPaused != _wasPaused) 
		{
			if (signal.IsPaused != _wasPaused)
			{
				_wasPaused = signal.IsPaused;
				_timeLabel.SelfModulate = _wasPaused ? Colors.Orange : Colors.White;
			}

			if (signal.TicksPassed != _lastTickPassed)
			{
				_lastTickPassed = signal.TicksPassed;
				long days = signal.TicksPassed / TicksPerDay;
				long ticks = signal.TicksPassed % TicksPerDay;
				_timeLabel.Text = $"Jour {days} - {ticks}t";
			}
		}
	}

	/// <summary>
	/// A method that makes the game interface automatically create a confirm dialog centered and sends the result
	/// back via another signal.
	/// </summary>
	public void HandleConfirmDialogSignal(ConfirmDialogSignal signal)
	{
		_dialog.DialogText = signal.DialogText;
		_dialog.Exclusive = true;
		_dialog.PopupCentered();
		
	}

	/// <summary>
	/// Sends a signal when the confirm dialog is answered
	/// </summary>
	private void OnDialogConfirmed()
	{
		Controller.Instance.Emit(new UISignal("OnConfirmExit"));
	}

	private void OnSpeedSelected(BaseButton button)
	{
		if (int.TryParse(button.Name, out int speed))
		{
			Controller.Instance.Emit(new SimulationSpeedSignal(speed));
		}
	}

	public void HandlePathfindingBakingSignal(PathfindBakingCompleteSignal signal)
	{
		foreach (BaseButton button in SpeedGroup.GetButtons())
		{
			button.SetDisabled(false);
		}
		
		_warningLabel.Visible = false;
	}

	public void OpenSaveNameDialog(Action<string, string> onSaveConfirmed, int simulationId, string pathDir = "res://_persistent/saves")
	{
		// warns when file already exists
		_saveNameDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		
		// only allows seeing files under res://
		_saveNameDialog.Access = FileDialog.AccessEnum.Resources;

		// objectively superior method of listing files. Fight me.
		_saveNameDialog.DisplayMode = FileDialog.DisplayModeEnum.List;
		
		_saveNameDialog.FavoritesEnabled = false;
		
		_saveNameDialog.ShowHiddenFiles = false;
		
		// file stuff
		
		var globalDir = ProjectSettings.GlobalizePath(pathDir);
		
		if (!Directory.Exists(globalDir)) Directory.CreateDirectory(globalDir);
		
		_saveNameDialog.CurrentDir = pathDir;
		_saveNameDialog.CurrentFile = 
			"simulation_" + simulationId + "_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");
		
		_saveNameDialog.Filters = ["*.json ; Scene Files"];
		ClearDialogConnections();
		
		_saveNameDialog.FileSelected += (string path) =>
		{
			// Godot's StringExtensions handle the path splitting natively
			string directoryPath = path.GetBaseDir();
			string fileNameWithExtension = path.GetFile();

			onSaveConfirmed?.Invoke(directoryPath, fileNameWithExtension);
		};
		GD.Print(_saveNameDialog.CurrentDir + ", " + _saveNameDialog.CurrentFile);
		_saveNameDialog.PopupCentered();
	}
	
	private void ClearDialogConnections()
	{
		// C# requires removing all specific lambda subscriptions, 
		// or you can use Godot's reflection to wipe them.
		var connections = _saveNameDialog.GetSignalConnectionList(FileDialog.SignalName.FileSelected);
		foreach (var connection in connections)
		{
			_saveNameDialog.Disconnect(FileDialog.SignalName.FileSelected, connection["callable"].AsCallable());
		}
	}
	
}
