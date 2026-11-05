public class RegisterFile: ISequentialLogic
{
    // data for 32 registers (0-31)

    #region INPUTS

    // WE3
    public bool WriteEnable { get; set; }

    // WD3
    public int WriteData { get; set; }

    // A3 (aka Rd)
    public int WriteAdress { get; set; }

    // A1, A2 (aka Rs1, Rs2)
    public int ReadAdress1 { get; set; }
    public int ReadAdress2 { get; set; }
    #endregion

    #region OUTPUTS

    // RD1, RD2
    public int ReadData1 { get; set; }
    public int ReadData2 { get; set; }
    #endregion

    public int[] Registers { get; private set; }

    #region Sequential logic

    public void PreClockUpdate() {
        ReadRegisters();
    }

    public void Clock() {
        if (WriteEnable) {
            if (WriteAdress < Registers.Length && WriteAdress >= 0) {
                Registers[WriteAdress] = WriteData;
            }
        }
    }
    #endregion

    public RegisterFile(bool writeEnable = true)
    {
        Registers = new int[32];
        WriteEnable = writeEnable;
    }

    public void InitializeRegisters(int[] values) {
        Registers = (int[])values.Clone();
    }

    // read of registers in Register file is O(1)
    public void ReadRegisters() {
        if (ReadAdress1 <= 0 || ReadAdress1 >= Registers.Length)
            ReadData1 = 0;
        else 
            ReadData1 = Registers[ReadAdress1];

        if (ReadAdress2 <= 0 || ReadAdress2 >= Registers.Length)
            ReadData2 = 0;
        else
            ReadData2 = Registers[ReadAdress2];
    }
}