/// <summary> View of an <see cref="Alu"/>: the four buttons set its operation, the banner shows the chosen one. </summary>
public class AluVisualiser : OperationSelectorVisualizer
{
    private Alu _alu;

    protected override string Title => "ALU";

    public AluVisualiser Bind(Alu alu)
    {
        Unbind();
        _alu = alu;
        _alu.OperationChanged += OnOperationChanged;
        return this;
    }

    protected override void OnOperationButton(int index)
    {
        if (_alu != null) _alu.Operation = (AluOperation)index;
    }

    private void OnOperationChanged(AluOperation operation)
    {
        ShowOperation(operation switch
        {
            AluOperation.ADD => "+",
            AluOperation.SUB => "-",
            AluOperation.AND => "&",
            AluOperation.OR => "|",
            _ => "?"
        });
    }

    private void Unbind()
    {
        if (_alu != null) _alu.OperationChanged -= OnOperationChanged;
        _alu = null;
    }

    private void OnDestroy() => Unbind();
}