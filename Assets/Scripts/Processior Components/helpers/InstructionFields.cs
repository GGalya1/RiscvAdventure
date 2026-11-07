// Register fields of a 32-bit RISC-V instruction word
public static class InstructionFields
{
    public static int Rd(int instruction) => (instruction >> 7) & 0x1F;    // [11:7]
    public static int Rs1(int instruction) => (instruction >> 15) & 0x1F;  // [19:15]
    public static int Rs2(int instruction) => (instruction >> 20) & 0x1F;  // [24:20]
}

// Start values shared by the processor levels
public static class ProcessorDefaults
{
    private static readonly int[] RegisterFileValues =
    {
        0, 1, 39, 43, 5, 6, 8, 40,
        3, 39, 13, 56, 63, 20, 50, 51,
        0, 12, 53, 65, 29, 60, 61, 0,
        25, 54, 0, 28, 70, 30, 31, 0
    };

    // A fresh copy of the default register file content
    public static int[] RegisterValues => (int[])RegisterFileValues.Clone();
}