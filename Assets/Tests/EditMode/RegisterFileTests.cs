using NUnit.Framework;

public class RegisterFileTests
{
    [Test]
    public void Constructor_CreatesThirtyTwoRegistersInitializedToZero()
    {
        var registerFile = new RegisterFile();

        Assert.AreEqual(32, registerFile.Registers.Length);
        Assert.IsTrue(System.Array.TrueForAll(registerFile.Registers, v => v == 0));
    }

    [Test]
    public void Constructor_WriteEnable_DefaultsToTrue()
    {
        Assert.IsTrue(new RegisterFile().WriteEnable);
        Assert.IsFalse(new RegisterFile(writeEnable: false).WriteEnable);
    }

    [Test]
    public void ReadRegisters_RegisterZero_AlwaysReadsAsZero()
    {
        // x0 is hardwired to zero in RISC-V: even if something is written to it,
        // reading it back must always yield 0.
        var registerFile = new RegisterFile();
        registerFile.WriteAdress = 0;
        registerFile.WriteData = 123;
        registerFile.WriteEnable = true;
        registerFile.Clock();

        registerFile.ReadAdress1 = 0;
        registerFile.ReadRegisters();

        Assert.AreEqual(0, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_ValidAddress_ReturnsStoredValue()
    {
        var values = new int[32];
        values[5] = 77;
        var registerFile = new RegisterFile();
        registerFile.InitializeRegisters(values);

        registerFile.ReadAdress1 = 5;
        registerFile.ReadRegisters();

        Assert.AreEqual(77, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_NegativeAddress_ReturnsZero()
    {
        var registerFile = new RegisterFile();

        registerFile.ReadAdress1 = -1;
        registerFile.ReadRegisters();

        Assert.AreEqual(0, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_AddressAtOrAboveThirtyTwo_ReturnsZero()
    {
        var registerFile = new RegisterFile();

        registerFile.ReadAdress1 = 32;
        registerFile.ReadRegisters();

        Assert.AreEqual(0, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_ReadsBothPortsIndependently()
    {
        var values = new int[32];
        values[1] = 10;
        values[2] = 20;
        var registerFile = new RegisterFile();
        registerFile.InitializeRegisters(values);

        registerFile.ReadAdress1 = 1;
        registerFile.ReadAdress2 = 2;
        registerFile.ReadRegisters();

        Assert.AreEqual(10, registerFile.ReadData1);
        Assert.AreEqual(20, registerFile.ReadData2);
    }

    [Test]
    public void Clock_WriteEnabled_StoresDataAtWriteAddress()
    {
        var registerFile = new RegisterFile();
        registerFile.WriteAdress = 3;
        registerFile.WriteData = 55;
        registerFile.WriteEnable = true;

        registerFile.Clock();

        Assert.AreEqual(55, registerFile.Registers[3]);
    }

    [Test]
    public void Clock_WriteDisabled_DoesNotModifyRegisters()
    {
        var registerFile = new RegisterFile();
        registerFile.WriteAdress = 3;
        registerFile.WriteData = 55;
        registerFile.WriteEnable = false;

        registerFile.Clock();

        Assert.AreEqual(0, registerFile.Registers[3]);
    }

    [TestCase(-1)]
    [TestCase(32)]
    public void Clock_WriteAddressOutOfRange_IsIgnoredSafely(int address)
    {
        var registerFile = new RegisterFile();
        registerFile.WriteAdress = address;
        registerFile.WriteData = 999;
        registerFile.WriteEnable = true;

        Assert.DoesNotThrow(registerFile.Clock);
    }

    [Test]
    public void WriteThenRead_AcrossTwoClockCycles_ReturnsWrittenValue()
    {
        var registerFile = new RegisterFile();

        // Cycle 1: write 42 into x7
        registerFile.WriteAdress = 7;
        registerFile.WriteData = 42;
        registerFile.WriteEnable = true;
        registerFile.Clock();

        // Cycle 2: read x7 back
        registerFile.ReadAdress1 = 7;
        registerFile.PreClockUpdate();

        Assert.AreEqual(42, registerFile.ReadData1);
    }

    [Test]
    public void InitializeRegisters_CopiesValues_AndDoesNotAliasSourceArray()
    {
        var source = new int[32];
        source[10] = 5;
        var registerFile = new RegisterFile();

        registerFile.InitializeRegisters(source);
        source[10] = 999; // mutate the original array after initialization

        Assert.AreEqual(5, registerFile.Registers[10]);
    }
}
