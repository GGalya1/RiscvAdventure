//  A pooled sphere that travels along a bus; clicking it shows the carried value
public class BusSignal : BaseVisualizer
{
    private InfoPanelUI _panel;

    protected override void InitializePanel()
    {
        _panel = RequirePanel<InfoPanelUI>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (_panel != null) _panel.Display("", "N/A");
    }

    public void ShowValue(int value)
    {
        _panel.Display("Signal", value);
    }

    //  Prepares the signal for returning to the pool
    public override void ResetVisualisation()
    {
        _panel.Display("", "N/A");
        HidePanelImmediately();
    }

    public override void SetInteractable(bool value)
    {
        // nothing to do here
    }
}