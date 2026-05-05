@tool
extends EditorScript

# Use Ctrl + Shift + X to run this script
func _run():
	var root = get_editor_interface().get_base_control()
	# Navigate to the editor root to find the hidden .NET plugin
	while root.get_parent() != null:
		root = root.get_parent()
	
	var dotnet_plugin = _find_dotnet_plugin(root)
	
	if dotnet_plugin:
		# Toggle the build setting
		var current_setting = dotnet_plugin.get("SkipBuildBeforePlaying")
		dotnet_plugin.set("SkipBuildBeforePlaying", !current_setting)
		
		var status = "DISABLED" if !current_setting else "ENABLED"
		print("C# Auto-Build is now: ", status)
	else:
		print("Could not find the C# plugin. Make sure your project is a .NET project.")

func _find_dotnet_plugin(node: Node) -> Node:
	if node.has_method("BuildProjectPressed"):
		return node
	for child in node.get_children():
		var found = _find_dotnet_plugin(child)
		if found: return found
	return null
