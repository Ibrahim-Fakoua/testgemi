using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Terrarium_Virtuel.scripts.database;
using Terrarium_Virtuel.signals.database;

public partial class ViewerToolBarMenu : MenuBar
{
	private Dictionary<int, string> _methodRegistry = new();
	private Dictionary<int, PopupMenu> _itemMenuRegistry = new();
	private Dictionary<string, int> _methodToIdLookup = new();
	private int _idCounter = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		DefineSignalListeners();
	}

	private void DefineSignalListeners()
	{
		Controller.Instance.Subscribe<SimulationStateSignal>(OnUIChange);
	}
	
	public override void _ExitTree()
	{
		// needs to be unsubscribed in order to prevent memory leaks and null node access errors
		Controller.Instance.Unsubscribe<SimulationStateSignal>(OnUIChange);
	}

	public void OnUIChange(SimulationStateSignal signal)
	{
		
	}
	
	public void InitializeFromConfig(string jsonPath)
	{
		if (!FileAccess.FileExists(jsonPath))
		{
			GD.PushError($"[ToolbarMenu] File {jsonPath} does not exist!");
			return;
		}
		
		foreach (Node child in GetChildren()) child.QueueFree();
		_methodRegistry.Clear();
		_itemMenuRegistry.Clear();
		_methodToIdLookup.Clear();
		_idCounter = 0;
		
		using var file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
		string jsonContent = file.GetAsText();
		var menuData = JsonSerializer.Deserialize<Dictionary<string, List<ToolbarItems>>>(jsonContent);

		foreach (var category in menuData)
		{
			var categoryName = category.Key;
			
			var popup = new PopupMenu { Name = categoryName, Title = categoryName };
			AddChild(popup);
			
			popup.IdPressed += OnItemIdPressed;
			PopulateSubMenu(popup, category.Value);
		}
	}

	private void PopulateSubMenu(PopupMenu parentPopup, List<ToolbarItems> subMenu)
	{
		foreach (var option in subMenu)
		{
			if (option.Type?.ToLower() == "seperator")
			{
				GD.PushError($"[MenuBar] The 'type' entry for '{option.Label}' is invalid. Learn to spell, bozo. (Hint: There is only one 'e'.");
				continue;
			}
			if (option.Type?.ToLower() == "separator" || option.Label == "-" || option.Label == "---")
			{
				parentPopup.AddSeparator();
				continue;
			}
			
			// submenu recursion
			if (option.SubMenu != null && option.SubMenu.Count > 0)
			{
				var subPopup = new PopupMenu { Name = option.Label };
				parentPopup.AddChild(subPopup);
				parentPopup.AddSubmenuNodeItem(option.Label, subPopup);
				
				subPopup.IdPressed += OnItemIdPressed; // links method calls (signals)
				
				PopulateSubMenu(subPopup, option.SubMenu); // keeps the recursion going if there is any
				continue;
			}
			
			int id = _idCounter++;
			
			if (option.Type?.ToLower() == "checkbox")
			{
				parentPopup.AddCheckItem(option.Label, id);
				parentPopup.SetItemChecked(parentPopup.GetItemIndex(id), option.DefaultValue);
			}
			else
			{
				parentPopup.AddItem(option.Label, id);
			}
			
			_itemMenuRegistry.Add(id, parentPopup);
			
			if (!string.IsNullOrEmpty(option.MethodName))
			{
				_methodRegistry.Add(id, option.MethodName);
				_methodToIdLookup[option.MethodName] = id;
			}
			else
			{
				GD.PushWarning($"[MenuBar] Method '{option.Label}' has no method.");
			}
		}
	}

	/*
	 * Calls an emit when a 
	 */
	private void OnItemIdPressed(long id)
	{
		int idInt = (int)id;
		
		_itemMenuRegistry[idInt].ToggleItemChecked(_itemMenuRegistry[idInt].GetItemIndex(idInt));
		if (_methodRegistry.TryGetValue(idInt, out string methodName))
		{
			Controller.Instance.Emit(new DBViewerSignal(methodName));
		}
	}


	public void DelegateHeartbeat(SimulationStateSignal signal)
	{
		if (_methodToIdLookup.TryGetValue("OnPauseGame", out int pauseId))
		{
			if (_itemMenuRegistry.TryGetValue(pauseId, out PopupMenu popupMenu))
			{
				int index = popupMenu.GetItemIndex(pauseId);
				popupMenu.SetItemChecked(index, signal.IsPaused);
			}
		}
	}
}
