using Godot;
using System;
using Terrarium_Virtuel.signals;

public partial class Main : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Controller.Instance.RegisterMainNode(this);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			Controller.Instance.Emit(new GracefulStopSignal());
			GetTree().Quit();
		}
	}
}

