using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class SettingsDialog : AcceptDialog
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Gets the start time
		ulong startTime = Time.GetTicksMsec();

		BuildFromConfig("res://config/ui/settings_config.json");
		DefineSignals();

		// Calculates the delta
		ulong duration = Time.GetTicksMsec() - startTime;
	
		GD.Print($"[Settings] Created Menu Settings Dialog in {duration}ms!!");
	}

	/// <summary>
	/// The method used to isolate the signal connections from _Ready() method. Simply for readability.
	/// It only defines methods that receive the signals, sending is another thing.
	/// </summary>
	private void DefineSignals()
	{
		Confirmed += _OnConfirmed;
	}
	
	/// <summary>
	/// A method that creates the node tree necessary for the settings dialog menu from the config file defined at the path received in argument.
	/// It is an entirely data-driven UI approach.
	/// </summary>
	/// <param name="configPath"></param>
	public void BuildFromConfig(string configPath)
	{
		var settingsData = ParseJson(configPath);
		if (settingsData == null || settingsData.Count == 0) GD.PrintErr("[Settings] Deserialization failed or JSON is empty.");
		
		
		var tabs = GetNode<TabContainer>("TabContainer");
		ClearExistingTabs(tabs);
		tabs.Visible = true;
		tabs.CustomMinimumSize = new Vector2(450, 600);
		
		foreach (var category in settingsData)
		{
			
			var categoryName = category.Key;
			var categoryOptions = category.Value;
			GD.Print($"[Settings] Building category: {categoryName}");
			BuildCategoryTab(tabs, categoryName,  categoryOptions);

		}
		// polish (cow)
		tabs.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize, 10);
		ResetSize(); 
	}

	private void BuildCategoryTab(TabContainer tabs, string categoryName, List<MenuOption> categoryOptions)
	{
		// Creates a scroll bar for each setting category
		var scroll = new ScrollContainer
		{
			Name = categoryName,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 150),
		};
		tabs.AddChild(scroll);
			
		// Creates a grid container for each category
		var grid = new GridContainer
		{
			Name = "Grid",
			Columns = 2,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		grid.AddThemeConstantOverride("h_separation", 20);
		grid.AddThemeConstantOverride("v_separation", 10);
			
		scroll.AddChild(grid);

		foreach (var option in categoryOptions)
		{

			if (!ValidateOption(option))
			{
				continue;
			}
			
			GD.Print($"[Settings] Adding: {option.Label} ({option.Type})");
			
			// Creates the label
			var label = new Label { Text = option.Label };
			grid.AddChild(label);
				
			// creates the widget according to type
			Control widget = CreateWidgetByType(option);
			
			// names the node
			widget.Name = option.ID;
			
			grid.AddChild(widget);
		}
	}

	private void ClearExistingTabs(TabContainer tabs)
	{
		foreach (Node child in tabs.GetChildren())
		{
			tabs.RemoveChild(child);
			child.QueueFree();
		}
	}

	/// <summary>
	/// The method that creates each input widget depending on it's type and data.
	/// It automatically generates the widget if it has all necessary data
	/// and gives progressive warnings about the 'completed-ness' of the config for each setting. 
	/// </summary>
	/// <param name="option"></param>
	/// <returns></returns>
	private Control CreateWidgetByType(MenuOption option)
	{

		if (!Enum.TryParse(option.Type, true, out SettingsWidgetType type))
		{
			type = SettingsWidgetType.Unknown;
		}
		
		
		// switch case for generating the input widget in the settings dialog
		switch (type)
		{
			case SettingsWidgetType.Toggle :
				return new CheckBox
				{
					ButtonPressed = GetValue<bool>(option.DefaultValue, false),
					TooltipText = option.Tooltip
				};
			
			case SettingsWidgetType.Slider :
				return new HSlider
				{
					MinValue = option.Min,
					MaxValue = option.Max,
					Step =  option.Step,
					Value = GetValue<double>(option.DefaultValue, option.Min), // defaults to min if not specified (YOU SHOULD)
					TooltipText = option.Tooltip,
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
				};
			
			case SettingsWidgetType.Spinbox:
				return new SpinBox
				{
					MinValue = option.Min,
					MaxValue = option.Max,
					Step =  option.Step,
					Value = GetValue<double>(option.DefaultValue, option.Min), // defaults to min if not specified (YOU SHOULD)
				};
			
			case SettingsWidgetType.Dropdown:
				var optionButton = new OptionButton { TooltipText = option.Tooltip };
				foreach (var item in option.Options)
					optionButton.AddItem(item.ToString());
				optionButton.Selected = GetValue<int>(option.DefaultValue, 0);
				return optionButton;
			
			case SettingsWidgetType.LineEdit:
			default:
				return new LineEdit
				{
					Text = GetValue<string>(option.DefaultValue, ""),
					TooltipText = option.Tooltip,
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
				};	
		}
	}

	/// <summary>
	/// A method that returns a dict containing the categories, elements and other nonsense that is required for making the interface.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private Dictionary<string, List<MenuOption>> ParseJson(string path)
	{
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"[Settings] File not found: {path}");
			return null;
		}
		
		// using makes the file stream close once the block is left. Prevents memory leaks or read/write access issues
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read); 
		string jsonContent = file.GetAsText();
		
		var settings = JsonSerializer.Deserialize<Dictionary<string, List<MenuOption>>>(jsonContent);
		
		if (settings == null || settings.Count == 0)
		{
			GD.PrintErr("[Settings] Deserialization failed or JSON is empty.");
			return null;
		}

		return settings;
	}

	private bool ValidateOption(MenuOption option)
	{
		// CRITICAL ERRORS : These break the Scraper and the generator
		if (string.IsNullOrEmpty(option.ID))
		{
			// If ID and Label are both missing, call it "Unnamed Option" so the log isn't empty
			string identifier = !string.IsNullOrEmpty(option.Label) ? option.Label : "Unknown Option";
			GD.PushError($"[Settings] CRITICAL: Missing 'id' for {identifier}. Widget skipped.");
			return false;
		}

		if (string.IsNullOrEmpty(option.Type))
		{
			GD.PushError($"[Settings] CRITICAL : Option 'type' for {option.Label}. Setting Widget is skipped.");
			return false;
		}
		
		// WARNINGS : These are less critical but should STILL be defined. Otherwise, shame on you.
		if (option.DefaultValue == null)
		{
			GD.PushWarning($"[Settings] Warning: ID '{option.ID}' has no 'default' key. Using system fallback.");
		}
		else if (option.DefaultValue is string str && string.IsNullOrWhiteSpace(str))
		{
			GD.PushWarning($"[Settings] Warning: ID '{option.ID}' has an empty string as default.");
		}
		
		// For dropdown widget verification.
		if (option.Type.ToLower() == "dropdown")
		{
			if (option.Options == null || option.Options.Count == 0)
			{
				GD.PushError($"[Settings] CRITICAL: Dropdown '{option.ID}' has no options defined!");
				return false;
			}
			// Optional: Warn if there's only one option (What's the point of a dropdown with one choice?)
			if (option.Options.Count == 1)
			{
				GD.PushWarning($"[Settings] Warning: Dropdown '{option.ID}' only has one option. Is this intended?");
			}
		}
		return true; // if nothing is wrong
	}
	

	
	
	/// <summary>
	/// The method called when the SettingsDialogBox sends the Confirmed signal, telling the controller to dispatch the new data to whatever is listening on the specified ChannelType
	/// </summary>
	private void _OnConfirmed()
	{
		var newData = ScrapeData();
		
		// Controller.Instance.Dispatch(ChannelTypes.GlobalSettings, newData);
		
		// GD.Print("[Settings] Confirmed");
	}

	private Dictionary<string, object> ScrapeData()
	{
		var batch = new Dictionary<string, object>();
		var tabs = GetNode<TabContainer>("TabContainer");

		foreach (var scroll in tabs.GetChildren())
		{
			var grid = scroll.GetChild<GridContainer>(0);
			foreach (var widget in grid.GetChildren())
			{
				if (widget is Label) continue;

				batch[widget.Name] = GetValueFromWidget(widget);
			}
		}
		// GD.Print($"[Settings] Scrape {batch.Count} settings.");
		return batch;
	}

	private object GetValueFromWidget(Node widget)
	{
		return widget switch
		{
			CheckBox cb => cb.ButtonPressed,
			HSlider hs => hs.Value,
			SpinBox sb => sb.Value,
			LineEdit le => le.Text,
			OptionButton ob => ob.Selected,
			_ => null
		};
		
	}
	
	private T GetValue<T>(object rawValue, T fallback)
	{
		if (rawValue is JsonElement element)
		{
			try 
			{
				return element.ValueKind switch
				{
					JsonValueKind.String => (T)(object)element.GetString(),
					JsonValueKind.Number => (T)Convert.ChangeType(element.GetDouble(), typeof(T)),
					JsonValueKind.True => (T)(object)true,
					JsonValueKind.False => (T)(object)false,
					_ => fallback
				};
			}
			catch { return fallback; }
		}
	
		// If it's already a native type (like during a manual refresh)
		try { return (T)Convert.ChangeType(rawValue, typeof(T)); }
		catch { return fallback; }
	}
}
