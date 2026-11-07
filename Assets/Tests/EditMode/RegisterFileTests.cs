using NUnit.Framework;

public class RegisterFileTests
{
    [Test]
    public void Constructor_CreatesThirtyTwoRegistersInitializedToZero()
    {
        var registerFile = new RegisterFile();

        Assert.AreEqual(32, registerFile.Registers.Count);
        CollectionAssert.AreEqual(new int[32], registerFile.Registers);
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
        registerFile.WriteAddress = 0;
        registerFile.WriteData = 123;
        registerFile.WriteEnable = true;
        registerFile.Clock();

        registerFile.ReadAddress1 = 0;
        registerFile.ReadRegisters();

        Assert.AreEqual(0, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_ValidAddress_ReturnsStoredValue()
    {
        var values = new int[32];
        values[5] = 77;
        var registerFile = new RegisterFile(values);

        registerFile.ReadAddress1 = 5;
        registerFile.ReadRegisters();

        Assert.AreEqual(77, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_NegativeAddress_ReturnsZero()
    {
        var registerFile = new RegisterFile();

        registerFile.ReadAddress1 = -1;
        registerFile.ReadRegisters();

        Assert.AreEqual(0, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_AddressAtOrAboveThirtyTwo_ReturnsZero()
    {
        var registerFile = new RegisterFile();

        registerFile.ReadAddress1 = 32;
        registerFile.ReadRegisters();

        Assert.AreEqual(0, registerFile.ReadData1);
    }

    [Test]
    public void ReadRegisters_ReadsBothPortsIndependently()
    {
        var values = new int[32];
        values[1] = 10;
        values[2] = 20;
        var registerFile = new RegisterFile(values);

        registerFile.ReadAddress1 = 1;
        registerFile.ReadAddress2 = 2;
        registerFile.ReadRegisters();

        Assert.AreEqual(10, registerFile.ReadData1);
        Assert.AreEqual(20, registerFile.ReadData2);
    }

    [Test]
    public void Clock_WriteEnabled_StoresDataAtWriteAddress()
    {
        var registerFile = new RegisterFile();
        registerFile.WriteAddress = 3;
        registerFile.WriteData = 55;
        registerFile.WriteEnable = true;

        registerFile.Clock();

        Assert.AreEqual(55, registerFile.Registers[3]);
    }

    [Test]
    public void Clock_WriteDisabled_DoesNotModifyRegisters()
    {
        var registerFile = new RegisterFile();
        registerFile.WriteAddress = 3;
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
        registerFile.WriteAddress = address;
        registerFile.WriteData = 999;
        registerFile.WriteEnable = true;

        Assert.DoesNotThrow(registerFile.Clock);
    }

    [Test]
    public void WriteThenRead_AcrossTwoClockCycles_ReturnsWrittenValue()
    {
        var registerFile = new RegisterFile();

        // Cycle 1: write 42 into x7
        registerFile.WriteAddress = 7;
        registerFile.WriteData = 42;
        registerFile.WriteEnable = true;
        registerFile.Clock();

        // Cycle 2: read x7 back
        registerFile.ReadAddress1 = 7;
        registerFile.PreClockUpdate();

        Assert.AreEqual(42, registerFile.ReadData1);
    }

    [Test]
    public void Constructor_CopiesInitialValues_AndDoesNotAliasSourceArray()
    {
        var source = new int[32];
        source[10] = 5;

        var registerFile = new RegisterFile(source);
        source[10] = 999; // mutate the original array after construction

        Assert.AreEqual(5, registerFile.Registers[10]);
    }

    [Test]
    public void Load_CopiesValues_AndDoesNotAliasSourceArray()
    {
        var source = new int[32];
        source[10] = 5;
        var registerFile = new RegisterFile();

        registerFile.Load(source);
        source[10] = 999; // mutate the original array after loading

        Assert.AreEqual(5, registerFile.Registers[10]);
    }

    [TestCase(0, 0)]    // x0 is hardwired to zero
    [TestCase(1, 11)]
    [TestCase(31, 41)]
    [TestCase(-1, 0)]   // out of range
    [TestCase(32, 0)]   // out of range
    public void ReadPort_FollowsReadPortRules(int address, int expected)
    {
        var registers = new int[32];
        registers[0] = 10; // even a non-zero x0 must read as 0
        registers[1] = 11;
        registers[31] = 41;

        Assert.AreEqual(expected, RegisterFile.ReadPort(registers, address));
    }

    // ---------------------------------------------------------------- snapshots for the tick history

    [Test]
    public void SaveRestore_RoundTripsValuesAndWriteEnable()
    {
        var values = new int[32];
        values[4] = 40;
        var registerFile = new RegisterFile(values, writeEnable: false);
        var saved = registerFile.Save();

        registerFile.WriteEnable = true;
        registerFile.WriteAddress = 4;
        registerFile.WriteData = 99;
        registerFile.Clock();
        registerFile.Restore(saved);

        Assert.AreEqual(40, registerFile.Registers[4]);
        Assert.IsFalse(registerFile.WriteEnable);
    }

    [Test]
    public void Save_IsACopy_NotAffectedByLaterWrites()
    {
        var registerFile = new RegisterFile();
        var saved = registerFile.Save();

        registerFile.WriteAddress = 1;
        registerFile.WriteData = 5;
        registerFile.Clock();

        Assert.AreEqual(0, saved.Values[1]);
    }

    [Test]
    public void Restore_DoesNotAliasTheSnapshot()
    {
        var registerFile = new RegisterFile();
        var saved = registerFile.Save();
        registerFile.Restore(saved);

        registerFile.WriteAddress = 1;
        registerFile.WriteData = 5;
        registerFile.Clock();

        Assert.AreEqual(0, saved.Values[1], "restoring twice from the same history entry must give the same state");
    }
}
