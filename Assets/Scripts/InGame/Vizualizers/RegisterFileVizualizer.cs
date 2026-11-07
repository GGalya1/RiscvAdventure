/// <summary> View of a <see cref="RegisterFile"/>: shows the register values, WE state and blinks on the clock edge. </summary>
public class RegisterFileVisualizer : SequentialVisualizer
{
    private RegisterFieldPanelUI _panel;
    private RegisterFile _registerFile;

    protected override void InitializePanel()
    {
        _panel = RequirePanel<RegisterFieldPanelUI>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (_panel != null) _panel.Display(new int[16]);
    }

    public RegisterFileVisualizer Bind(RegisterFile registerFile)
    {
        _registerFile = registerFile;
        BindModel(registerFile);
        return this;
    }

    protected override void RefreshContent()
    {
        _panel.Display(_registerFile.Registers);
    }
}