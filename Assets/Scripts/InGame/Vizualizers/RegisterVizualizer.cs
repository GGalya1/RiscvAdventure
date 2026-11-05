using System;

/// <summary>
/// View of a <see cref="Register"/>: shows its value in the info panel, the WE state on the model
/// and blinks on the clock edge (see <see cref="SequentialVisualizer"/>).
/// </summary>
public class RegisterVisualizer : SequentialVisualizer
{
    private RegisterControlPanel _panel;
    private Register _register;
    private string _title;
    private Func<int, string> _format;

    protected override void InitializePanel()
    {
        _panel = RequirePanel<RegisterControlPanel>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (_panel != null) _panel.Display("N/A", 0);
    }

    /// <param name="register">Simulation register shown by this view.</param>
    /// <param name="title">Panel title, e.g. "PC Register".</param>
    /// <param name="format">Optional text for the value (e.g. a disassembled instruction); the plain number otherwise.</param>
    public RegisterVisualizer Bind(Register register, string title, Func<int, string> format = null)
    {
        _register = register;
        _title = title;
        _format = format;
        BindModel(register);
        return this;
    }

    protected override void RefreshContent()
    {
        if (_format == null)
            _panel.Display(_title, _register.Output);
        else
            _panel.Display(_title, _format(_register.Output));
    }
}