using System;

/// <summary>
/// Control signal of the extend unit (ImmSrc). The numeric values are the button indices
/// in the extender panel and the values stored in level snapshots.
/// </summary>
public enum ImmediateType
{
    I = 0,
    S = 1,
    B = 2,
    J = 3,
}

/// <summary>
/// Sign-extend unit: assembles the immediate of a RISC-V instruction according to
/// its control signal <see cref="Type"/>.
/// </summary>
public sealed class Extender
{
    private ImmediateType _type = ImmediateType.I;

    /// <summary> Raised every time the control signal is assigned. </summary>
    public event Action<ImmediateType> TypeChanged;

    public ImmediateType Type
    {
        get => _type;
        set
        {
            _type = value;
            TypeChanged?.Invoke(value);
        }
    }

    /// <summary> Immediate of <paramref name="instruction"/> for the current control signal. </summary>
    public int Extend(int instruction) => Evaluate(_type, (uint)instruction);

    public static int Evaluate(ImmediateType type, uint instruction)
    {
        switch (type)
        {
            case ImmediateType.I:
                return (int)instruction >> 20;

            case ImmediateType.S:
                var immS = (int)(((instruction >> 25) << 5) | ((instruction >> 7) & 0x1F));
                return SignExtend(immS, 12);

            case ImmediateType.B:
                return SignExtend(AssembleB(instruction), 13);

            case ImmediateType.J:
                return SignExtend(AssembleJ(instruction), 21);

            default:
                return 0;
        }
    }

    private static int AssembleB(uint val)
    {
        var assembled = (((val >> 31) & 0x1) << 12) |   // bit 31     -> imm[12]
                        (((val >> 7) & 0x1) << 11) |    // bit 7      -> imm[11]
                        (((val >> 25) & 0x3F) << 5) |   // bits 30:25 -> imm[10:5]
                        (((val >> 8) & 0xF) << 1);      // bits 11:8  -> imm[4:1]
        return (int)assembled;
    }

    private static int AssembleJ(uint val)
    {
        var assembled = ((val >> 31) << 20) |           // bit 31     -> imm[20]
                        (((val >> 12) & 0xFF) << 12) |  // bits 19:12 -> imm[19:12]
                        (((val >> 20) & 0x1) << 11) |   // bit 20     -> imm[11]
                        (((val >> 21) & 0x3FF) << 1);   // bits 30:21 -> imm[10:1]
        return (int)assembled;
    }

    private static int SignExtend(int value, int bitCount)
    {
        var signBit = 1 << (bitCount - 1);
        if ((value & signBit) != 0)
        {
            return value | (unchecked((int)0xFFFFFFFF) << bitCount);
        }
        return value;
    }
}
