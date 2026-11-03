// Visualizer of the register file: register values, WE state, blinking on the clock edge
public class RegisterFileVisualizer : SequentialVisualizer
{
    public RegisterFieldPanelUI UIRegisterPanel { get; private set; }

    protected override void InitializePanel()
    {
        UIRegisterPanel = RequirePanel<RegisterFieldPanelUI>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (UIRegisterPanel != null) UIRegisterPanel.Display(new int[16]);
    }

    public override void ResetVisualisation()
    {
        throw new System.NotImplementedException();
    }
}