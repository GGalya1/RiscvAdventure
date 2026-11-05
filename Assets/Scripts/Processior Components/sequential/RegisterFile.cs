using System.Collections.Generic;

/// <summary>
/// RISC-V register file: two read ports (A1/RD1, A2/RD2) and one write port (A3/WD3/WE3).
/// <br/>Reads happen in <see cref="PreClockUpdate"/> (or explicitly via <see cref="ReadRegisters"/>),
/// the write happens on the clock edge.
/// </summary>
public sealed class RegisterFile : ISequentialLogic
{
    public const int RegisterCount = 32;

    #region INPUTS
    // WE3
    public bool WriteEnable { get; set; }

    // WD3
    public int WriteData { get; set; }

    // A3 (rd)
    public int WriteAddress { get; set; }

    // A1, A2 (rs1, rs2)
    public int ReadAddress1 { get; set; }
    public int ReadAddress2 { get; set; }
    #endregion

    #region OUTPUTS
    // RD1, RD2
    public int ReadData1 { get; private set; }
    public int ReadData2 { get; private set; }
    #endregion

    private int[] _registers;

    public IReadOnlyList<int> Registers => _registers;

    public RegisterFile(int[] initialValues = null, bool writeEnable = true)
    {
        _registers = initialValues != null ? (int[])initialValues.Clone() : new int[RegisterCount];
        WriteEnable = writeEnable;
    }

    public void Load(int[] values)
    {
        _registers = (int[])values.Clone();
    }

    #region Sequential logic
    public void PreClockUpdate()
    {
        ReadRegisters();
    }

    public void Clock()
    {
        if (WriteEnable && WriteAddress >= 0 && WriteAddress < _registers.Length)
        {
            _registers[WriteAddress] = WriteData;
        }
    }
    #endregion

    /// <summary> Updates RD1/RD2 from A1/A2 immediately (combinational read). </summary>
    public void ReadRegisters()
    {
        ReadData1 = ReadPort(_registers, ReadAddress1);
        ReadData2 = ReadPort(_registers, ReadAddress2);
    }

    /// <summary> Read port semantics: x0 and out-of-range addresses read as 0. </summary>
    public static int ReadPort(IReadOnlyList<int> registers, int address)
    {
        return address <= 0 || address >= registers.Count ? 0 : registers[address];
    }

    public RegisterFileState Save() => new((int[])_registers.Clone(), WriteEnable);

    public void Restore(RegisterFileState state)
    {
        Load(state.Values);
        WriteEnable = state.WriteEnable;
    }
}

/// <summary> Snapshot of a <see cref="RegisterFile"/> for the tick history. </summary>
public readonly struct RegisterFileState
{
    public readonly int[] Values;
    public readonly bool WriteEnable;

    public RegisterFileState(int[] values, bool writeEnable)
    {
        Values = values;
        WriteEnable = writeEnable;
    }
}
