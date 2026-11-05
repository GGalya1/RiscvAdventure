/// <summary>
/// RISC-V register file: two read ports (A1/RD1, A2/RD2) and one write port (A3/WD3/WE3).
/// <br/>Reads happen in <see cref="PreClockUpdate"/> (or explicitly via <see cref="ReadRegisters"/>),
/// the write happens on the clock edge.
/// </summary>
public class RegisterFile : ISequentialLogic
{
    // data for 32 registers (0-31)
    public const int RegisterCount = 32;

    #region INPUTS
    // WE3
    public bool WriteEnable { get; set; }

    // WD3
    public int WriteData { get; set; }

    // A3 (aka Rd)
    public int WriteAddress { get; set; }

    // A1, A2 (aka Rs1, Rs2)
    public int ReadAddress1 { get; set; }
    public int ReadAddress2 { get; set; }
    #endregion

    #region OUTPUTS
    // RD1, RD2
    public int ReadData1 { get; private set; }
    public int ReadData2 { get; private set; }
    #endregion

    public int[] Registers { get; private set; }

    public RegisterFile(int[] initialValues = null, bool writeEnable = true)
    {
        Registers = initialValues != null ? (int[])initialValues.Clone() : new int[RegisterCount];
        WriteEnable = writeEnable;
    }

    public void Load(int[] values)
    {
        Registers = (int[])values.Clone();
    }

    #region Sequential logic
    public void PreClockUpdate()
    {
        ReadRegisters();
    }

    public void Clock()
    {
        if (WriteEnable && WriteAddress >= 0 && WriteAddress < Registers.Length)
        {
            Registers[WriteAddress] = WriteData;
        }
    }
    #endregion

    // Updates RD1/RD2 from A1/A2 immediately (combinational read)
    public void ReadRegisters()
    {
        ReadData1 = ReadPort(Registers, ReadAddress1);
        ReadData2 = ReadPort(Registers, ReadAddress2);
    }

    // Read port semantics: x0 and out-of-range addresses read as 0
    public static int ReadPort(int[] registers, int address)
    {
        return address <= 0 || address >= registers.Length ? 0 : registers[address];
    }
}