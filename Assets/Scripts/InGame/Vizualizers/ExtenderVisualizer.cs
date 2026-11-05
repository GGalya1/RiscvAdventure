/// <summary> View of an <see cref="Extender"/>: the four buttons set the immediate type (I/S/B/J). </summary>
public class ExtenderVisualizer : OperationSelectorVisualizer
{
    private Extender _extender;

    protected override string Title => "Extender";

    public ExtenderVisualizer Bind(Extender extender)
    {
        Unbind();
        _extender = extender;
        _extender.TypeChanged += OnTypeChanged;
        return this;
    }

    protected override void OnOperationButton(int index)
    {
        if (_extender != null) _extender.Type = (ImmediateType)index;
    }

    private void OnTypeChanged(ImmediateType type)
    {
        ShowOperation(type switch
        {
            ImmediateType.I => "I",     // I-type (lw, addi)
            ImmediateType.S => "S",     // S-type (store)
            ImmediateType.B => "B",     // B-type (branch)
            ImmediateType.J => "J",     // J-type (jump)
            _ => "?"
        });
    }

    private void Unbind()
    {
        if (_extender != null) _extender.TypeChanged -= OnTypeChanged;
        _extender = null;
    }

    private void OnDestroy() => Unbind();
}