/// <summary>
/// A basic register for synchronous logic simulation.
/// <br/>On a clock edge the register stores <see cref="Input"/> in <see cref="Output"/>,
/// but only if <see cref="WriteEnable"/> is set.
/// </summary>
public sealed class Register : ISequentialLogic
{
    /// <summary> Value coming from the combinational logic; stored on the next clock edge. </summary>
    public int Input { get; set; }

    /// <summary> Stored value that drives the connected combinational blocks. </summary>
    public int Output { get; private set; }

    public bool WriteEnable { get; set; }

    // value sampled in PreClockUpdate and latched in Clock
    private int _nextValue;

    public Register(int initialValue = 0, bool writeEnable = true)
    {
        Output = initialValue;
        _nextValue = initialValue;
        WriteEnable = writeEnable;
    }

    public void PreClockUpdate()
    {
        _nextValue = WriteEnable ? Input : Output;
    }

    public void Clock()
    {
        Output = _nextValue;
    }

    /// <summary> Overwrites the stored value without a clock edge. <see cref="Input"/> is kept. </summary>
    public void Reset(int value)
    {
        Output = value;
        _nextValue = value;
    }

    public RegisterState Save() => new(Output, WriteEnable);

    public void Restore(RegisterState state)
    {
        Reset(state.Value);
        WriteEnable = state.WriteEnable;
    }
}

/// <summary> Snapshot of a <see cref="Register"/> for the tick history. </summary>
public readonly struct RegisterState
{
    public readonly int Value;
    public readonly bool WriteEnable;

    public RegisterState(int value, bool writeEnable)
    {
        Value = value;
        WriteEnable = writeEnable;
    }
}