namespace Terrarium_Virtuel.signals;

public partial class ConfirmDialogSignal : SimulationSignal
{
    public string DialogText { get; }
    
    public ConfirmDialogSignal(string dialogText)
    {
        DialogText = dialogText;
    }
}