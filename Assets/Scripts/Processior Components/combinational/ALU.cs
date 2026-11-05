using System;

/// <summary>
/// Arithmetic logic unit: a combinational block with one control signal (<see cref="Operation"/>).
/// <br/>The object only stores the control signal; the computation itself is the pure function
/// <see cref="Calculate"/>, so it can also be evaluated for saved (historical) control values.
/// </summary>
public sealed class Alu
{
    private AluOperation _operation = AluOperation.ADD;

    /// <summary>
    /// Raised every time the control signal is assigned (also when the value does not change),
    /// so a view can react to both player input and history restore.
    /// </summary>
    public event Action<AluOperation> OperationChanged;

    public AluOperation Operation
    {
        get => _operation;
        set
        {
            _operation = value;
            OperationChanged?.Invoke(value);
        }
    }

    /// <summary> Result of the ALU for the current control signal. </summary>
    public int Compute(int a, int b) => Calculate(a, b, _operation);

    public static int Calculate(int a, int b, AluOperation operation) => operation switch
    {
        AluOperation.ADD => a + b,
        AluOperation.SUB => a - b,
        AluOperation.AND => a & b,
        AluOperation.OR => a | b,
        _ => 0
    };
}