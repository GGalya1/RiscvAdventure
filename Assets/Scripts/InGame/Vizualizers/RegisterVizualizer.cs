/*
 Visualizer of a digital register: shows its value in the info panel, the WE state on the model
 and blinks on the clock edge (see SequentialVisualizer).
*/
public class RegisterVisualizer : SequentialVisualizer
{
    // Panel used by the level to display the register value
    public RegisterControlPanel UIRegisterPanel { get; private set; }

    protected override void InitializePanel()
    {
        UIRegisterPanel = RequirePanel<RegisterControlPanel>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (UIRegisterPanel != null) UIRegisterPanel.Display("N/A", 0);
    }
}