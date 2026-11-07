public static class RiscVDecoder
{
    public static string CommandBuilder(uint val)
    {
        if (val < GameConstants.MinValidInstruction) return val.ToString();

        var opcode = val & 0x7F;
        var rd = (val >> 7) & 0x1F;
        var funct3 = (val >> 12) & 0x7;
        var rs1 = (val >> 15) & 0x1F;
        var rs2 = (val >> 20) & 0x1F;
        var funct7 = (val >> 25) & 0x7F;

        return opcode switch
        {
            0x33 => DecodeRType(funct3, funct7, rd, rs1, rs2),
            0x13 => DecodeITypeAlu(funct3, rd, rs1, (int)val >> 20),
            0x03 => $"lw x{rd}, {(int)val >> 20}(x{rs1})",
            0x23 => $"sw x{rs2}, {Extender.Evaluate(ImmediateType.S, val)}(x{rs1})",
            0x63 => $"beq x{rs1}, x{rs2}, {Extender.Evaluate(ImmediateType.B, val)}",
            0x6F => $"jal x{rd}, {Extender.Evaluate(ImmediateType.J, val)}",
            0x67 => $"jalr x{rd}, {(int)val >> 20}(x{rs1})",
            _ => $"unknown (0x{val:X8})"
        };
    }

    private static string DecodeRType(uint f3, uint f7, uint rd, uint rs1, uint rs2)
    {
        var op = (f3, f7) switch
        {
            (0x0, 0x00) => "add",
            (0x0, 0x20) => "sub",
            (0x7, 0x00) => "and",
            _ => "unknown_R"
        };
        return $"{op} x{rd}, x{rs1}, x{rs2}";
    }

    private static string DecodeITypeAlu(uint f3, uint rd, uint rs1, int imm)
    {
        // For shift instructions (SLLI, SRLI, SRAI), only the lower 5 or 6 bits of the imm
        // and the 30th bit of the instruction are used to distinguish between SRLI and SRAI.
        var shamt = (uint)imm & 0x1F; // shift amount
        var bit30 = ((imm >> 10) & 0x1) == 1; // The 30th bit of the entire instruction

        return f3 switch
        {
            0x0 => $"addi x{rd}, x{rs1}, {imm}", // Add Immediate
            0x1 => $"slli x{rd}, x{rs1}, {shamt}", // Shift Left Logical Imm
            0x2 => $"slti x{rd}, x{rs1}, {imm}", // Set Less Than Imm
            0x3 => $"sltiu x{rd}, x{rs1}, {(uint)imm}", // Set Less Than Imm Unsigned
            0x4 => $"xori x{rd}, x{rs1}, {imm}", // Xor Immediate
            0x5 => bit30 // Shift Right
                ? $"srai x{rd}, x{rs1}, {shamt}" // Arithmetic (saving sign)
                : $"srli x{rd}, x{rs1}, {shamt}", // Logical (filling with zeros)
            0x6 => $"ori x{rd}, x{rs1}, {imm}", // Or Immediate
            0x7 => $"andi x{rd}, x{rs1}, {imm}", // And Immediate
            _ => $"unknown_I x{rd}, x{rs1}, {imm}"
        };
    }
}