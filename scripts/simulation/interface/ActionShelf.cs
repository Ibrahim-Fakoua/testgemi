using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot.Collections;
using Terrarium_Virtuel.signals;
using Terrarium_Virtuel.signals.action_items;
using Array = Godot.Collections.Array;

// Using aliases to avoid namespace conflicts
using GDictionary = Godot.Collections.Dictionary;
using GArray = Godot.Collections.Array;

public partial class ActionShelf : TabContainer
{
	public static ActionShelf Instance;
	
	private string _currentCategory = "";
	
	private ActionItem _currentActionItem = null;
	
	private Godot.Collections.Dictionary<string, HFlowContainer> _actionShelves = new();
	public Godot.Collections.Dictionary<string, Array<ActionItem>> CategoryItems = new();
	private Godot.Collections.Dictionary<string, CheckButton> _brushModeButtons = new();
	
	[Export] public ButtonGroup ActionButtons;
	
	private int _clickCounter = 0;
	
	private bool _brushMode = false;
	
	public override void _Ready()
	{
		Instance = this;
		DefineSignals();
		
		// Define initial categories from the start
		// These will be populated by the JSON or signals later
		EnsureCategoryExists("Espèces");
		EnsureCategoryExists("Nourriture");
	}
	
	public override void _ExitTree()
	{
		Controller.Instance.Unsubscribe<ActionItemSignal>(NewActionItem);
	}

	private void DefineSignals()
	{
		// Subscribe to your Controller's signal bus
		Controller.Instance.Subscribe<ActionItemSignal>(NewActionItem);
		Controller.Instance.Subscribe<NewSpeciesSignal>(OnNewSpeciesSignal);
		Controller.Instance.Subscribe<PathfindBakingCompleteSignal>(HandlePathfindingSignal);
		
		TabChanged += OnTabChanged;
	}

	private void HandlePathfindingSignal(PathfindBakingCompleteSignal obj)
	{
		foreach (var button in ActionButtons.GetButtons())
		{
			button.SetDisabled(false);
		}
	}

	/// <summary>
	/// When the tabs are changed, it toggles the saved reference to the clicked action item if it wasnt already.
	/// </summary>
	/// <param name="tab"></param>
	private void OnTabChanged(long tab)
	{
		_clickCounter++;
		_currentActionItem = null;
		ToggleButtonsOff();
	}

	public void OnNewSpeciesSignal(NewSpeciesSignal signal)
	{
		ActionItem newSpecies = new ActionItem(signal.Id, signal.Label, "Just another species");

		ActionItemSignal newSignal = new ActionItemSignal(signal.Id, newSpecies);
		
		// we do not bypass the controller
		Controller.Instance.Emit(newSignal);
	}
	
	/// <summary>
	/// Method that ensures a category exists. If it dont exist, it creates it.
	/// </summary>
	/// <param name="label"></param>
	private void EnsureCategoryExists(string label)
	{
		if (!_actionShelves.ContainsKey(label))
		{
			var container = new HFlowContainer { Name = label, ReverseFill =  true };
			container.AddThemeConstantOverride("v_separation", 6);
			container.AddThemeConstantOverride("h_separation", 6);
			
			_actionShelves[label] = container;
			AddChild(container);

			var toggleButton = new CheckButton
			{
				Text = "Ajout Rapide",
				TooltipText = "Permet d'ajouter plus que une creatures facilement",
				Name = "toggle",
				ToggleMode = true
			};
			container.AddChild(toggleButton);
			
			toggleButton.Toggled += (toggled) =>
			{
				_brushMode = toggled;
				toggleButton.SelfModulate = toggled ? Colors.Orange : Colors.White;
			};
			
			_brushModeButtons[label] = toggleButton;
		}
		
		if (!CategoryItems.ContainsKey(label))
		{
			CategoryItems[label] = new Godot.Collections.Array<ActionItem>();
		}
	}

	private void NewActionItem(ActionItemSignal signal)
	{
		if (signal.ActionItem == null) return;
		
		ActionItem newItem = new ActionItem(
			signal.ActionItem.ID,
			signal.ActionItem.Label,
			signal.ActionItem.TooltipText
		);
		
		newItem.Category = signal.CategoryName;
	
		AddItemToCategory(newItem, newItem.Category ?? "Miscellaneous");
	}
	
	private void AddItemToCategory(ActionItem item, string category)
	{
		EnsureCategoryExists(category);

		CategoryItems[category].Add(item);
		
		Button itemButton = new Button
		{
			Text = item.Label,
			TooltipText = item.TooltipText,
			ToggleMode = true,
			Disabled = true
		};

		if (ActionButtons != null)
		{
			itemButton.ButtonGroup = ActionButtons;
		}
		
		itemButton.Pressed += () => ProcessClick(item);
		_actionShelves[category].AddChild(itemButton);
	}
	
	/// <summary>
	/// Simply processes the clicked button so it's reference is in memory.
	/// </summary>
	/// <param name="item"></param>
	private void ProcessClick(ActionItem item)
	{
		_currentActionItem = item;
	}

	/// <summary>
	/// ButtonGroup magical doohicky
	/// </summary>
	private void ToggleButtonsOff()
	{
		BaseButton button = ActionButtons.GetPressedButton();
		if (ActionButtons != null && button != null)
		{
			button.SetPressedNoSignal(false);
		}

		foreach (var (_, btn) in _brushModeButtons)
		{
			btn.SetPressedNoSignal(false);
			btn.SelfModulate = Colors.White;
		}
	}

	/// <summary>
	/// The method the GameInterface script
	/// </summary>
	/// <returns></returns>
	public ActionItem GetAndEmptyAction()
	{
		ActionItem item = _currentActionItem;
		
		if (!_brushMode)
		{
			ToggleButtonsOff();
			_currentActionItem = null;
		}
		return item;
	}

	/// <summary>
	/// Simply initializes from the config json file.
	/// </summary>
	/// <param name="jsonPath"></param>
	public void InitializeFromConfig(string jsonPath)
	{
		if (!FileAccess.FileExists(jsonPath)) return;

		using var file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
		string jsonContent = file.GetAsText();

		// Map the JSON directly to a C# Dictionary
		var menuData = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, List<ActionItem>>>(jsonContent);

		if (menuData == null) return;

		foreach (var category in menuData)
		{
			string categoryName = category.Key;
			EnsureCategoryExists(categoryName);
			
			foreach (ActionItem item in category.Value)
			{
				item.Category = categoryName;
				AddItemToCategory(item, categoryName);
			}
		}
	}

	/// <summary>
	/// Empty, future method for per-update-tick stats and interface updating.
	/// </summary>
	/// <param name="signal"></param>
	public void DelegateHeartbeat(SimulationStateSignal signal)
	{
		// does shit
	}
}
