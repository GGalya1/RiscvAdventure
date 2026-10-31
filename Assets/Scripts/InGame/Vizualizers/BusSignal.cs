using UnityEngine;

public class BusSignal : BaseVisualizer
{
    public InfoPanelUI UIRegisterPanel { get; private set; }

    protected override void Awake()
    {
        // panelLocalOffset = new Vector3(0, 0.8f, 0);

        base.Awake();

        if (UIRegisterPanel != null)
        {
            UIRegisterPanel.Display("", "N/A");
        }
    }

    public override void ResetVisualisation()
    {
        UIRegisterPanel.Display("", "N/A");
        HidePanelImmediately();
    }

    protected override void InitializePanel()
    {
        UIRegisterPanel = RequirePanel<InfoPanelUI>();
    }

    public override void SetInteractable(bool value)
    {
        // nothing to do here
    }
}