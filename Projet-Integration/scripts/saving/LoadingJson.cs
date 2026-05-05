using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Terrarium_Virtuel.signals;


namespace Terrarium_Virtuel.scripts.saving;

public class LoadingJson
{
	
	
	
	
	public static SaveData LoadJsonSave(string jsonPath)
	{
		if (!FileAccess.FileExists(jsonPath))
		{
			GD.PushError($"[LoadingJson] File {jsonPath} does not exist!");
			return null;
		}
		

		
		using var file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
		string jsonContent = file.GetAsText();
		var saveData = JsonSerializer.Deserialize<SaveData>(jsonContent);

		return saveData;
	}
}
