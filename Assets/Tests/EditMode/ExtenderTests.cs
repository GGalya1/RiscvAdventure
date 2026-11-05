using NUnit.Framework;

public class ExtenderTests
{
    // I-type: imm[11:0] -> instruction bits [31:20]
    private static uint BuildIType(int imm)
    {
        uint imm12 = (uint)imm & 0xFFF;
        return imm12 << 20;
    }

    // S-type: imm[11:5] -> bits [31:25], imm[4:0] -> bits [11:7]
    private static uint BuildSType(int imm)
    {
        uint imm12 = (uint)imm & 0xFFF;
        uint imm11_5 = (imm12 >> 5) & 0x7F;
        uint imm4_0 = imm12 & 0x1F;
        return (imm11_5 << 25) | (imm4_0 << 7);
    }

    // B-type: imm[12]->bit31, imm[11]->bit7, imm[10:5]->bits[30:25], imm[4:1]->bits[11:8] (bit 0 implicit 0)
    private static uint BuildBType(int imm)
    {
        uint imm13 = (uint)imm & 0x1FFF;
        uint bit12 = (imm13 >> 12) & 0x1;
        uint bit11 = (imm13 >> 11) & 0x1;
        uint bits10_5 = (imm13 >> 5) & 0x3F;
        uint bits4_1 = (imm13 >> 1) & 0xF;
        return (bit12 << 31) | (bits10_5 << 25) | (bits4_1 << 8) | (bit11 << 7);
    }

    // J-type: imm[20]->bit31, imm[19:12]->bits[19:12], imm[11]->bit20, imm[10:1]->bits[30:21] (bit 0 implicit 0)
    private static uint BuildJType(int imm)
    {
        uint imm21 = (uint)imm & 0x1FFFFF;
        uint bit20 = (imm21 >> 20) & 0x1;
        uint bits10_1 = (imm21 >> 1) & 0x3FF;
        uint bit11 = (imm21 >> 11) & 0x1;
        uint bits19_12 = (imm21 >> 12) & 0xFF;
        return (bit20 << 31) | (bits10_1 << 21) | (bit11 << 20) | (bits19_12 << 12);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(-1)]
    [TestCase(2047)]   // max positive value representable in 12 bits
    [TestCase(-2048)]  // min negative value
    public void Evaluate_IType_SignExtendsCorrectly(int imm)
    {
        var word = BuildIType(imm);

        var result = Extender.Evaluate(ImmediateType.I, word);

        Assert.AreEqual(imm, result);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(-1)]
    [TestCase(2047)]
    [TestCase(-2048)]
    [TestCase(1234)]
    public void Evaluate_SType_SignExtendsCorrectly(int imm)
    {
        var word = BuildSType(imm);

        var result = Extender.Evaluate(ImmediateType.S, word);

        Assert.AreEqual(imm, result);
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(-2)]
    [TestCase(4094)]   // max positive branch offset (13-bit, always even)
    [TestCase(-4096)]  // min negative branch offset
    public void Evaluate_BType_SignExtendsCorrectly(int imm)
    {
        var word = BuildBType(imm);

        var result = Extender.Evaluate(ImmediateType.B, word);

        Assert.AreEqual(imm, result);
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(-2)]
    [TestCase(1048574)]   // max positive jump offset (21-bit, always even)
    [TestCase(-1048576)]  // min negative jump offset
    public void Evaluate_JType_SignExtendsCorrectly(int imm)
    {
        var word = BuildJType(imm);

        var result = Extender.Evaluate(ImmediateType.J, word);

        Assert.AreEqual(imm, result);
    }

    [Test]
    public void Evaluate_UnknownType_ReturnsZero()
    {
        // Any control value outside I/S/B/J has no defined immediate format.
        var result = Extender.Evaluate((ImmediateType)99, 0xFFFFFFFF);

        Assert.AreEqual(0, result);
    }

    // The numeric value of ImmediateType is the button index in the extender panel.
    [TestCase(0, ImmediateType.I)]
    [TestCase(1, ImmediateType.S)]
    [TestCase(2, ImmediateType.B)]
    [TestCase(3, ImmediateType.J)]
    public void ButtonIndex_MapsToImmediateType(int buttonIndex, ImmediateType expected)
    {
        Assert.AreEqual(expected, (ImmediateType)buttonIndex);
    }
}
