using NUnit.Framework;

public class DataInstMemoryTests
{
    [Test]
    public void PreClockUpdate_UnwrittenAddress_ReadsAsZero()
    {
        var memory = new DataInstMemory { Address = 10 };

        memory.PreClockUpdate();

        Assert.AreEqual(0, memory.ReadData);
    }

    [Test]
    public void Constructor_WriteEnable_DefaultsToTrue()
    {
        Assert.IsTrue(new DataInstMemory().WriteEnable);
        Assert.IsFalse(new DataInstMemory(writeEnable: false).WriteEnable);
    }

    [Test]
    public void Memory_IsClockedThroughISequentialLogic()
    {
        var memory = new DataInstMemory { Address = 4, WriteData = 5, WriteEnable = true };
        ISequentialLogic element = memory;

        element.PreClockUpdate();
        element.Clock();
        element.PreClockUpdate();

        Assert.AreEqual(5, memory.ReadData);
    }

    [Test]
    public void LoadWord_ThenRead_ReturnsLoadedValue()
    {
        var memory = new DataInstMemory();
        memory.LoadWord(4, 777);

        memory.Address = 4;
        memory.PreClockUpdate();

        Assert.AreEqual(777, memory.ReadData);
    }

    [Test]
    public void Clock_WriteEnabled_StoresDataAtAddress()
    {
        var memory = new DataInstMemory { Address = 8, WriteData = 42, WriteEnable = true };

        memory.Clock();

        Assert.AreEqual(42, memory.Memory[8]);
    }

    [Test]
    public void Clock_WriteDisabled_DoesNotStoreData()
    {
        var memory = new DataInstMemory { Address = 8, WriteData = 42, WriteEnable = false };

        memory.Clock();

        Assert.IsFalse(memory.Memory.ContainsKey(8));
    }

    [Test]
    public void PreClockUpdate_ReadsIndependentlyOfWriteEnable()
    {
        var memory = new DataInstMemory();
        memory.LoadWord(2, 15);

        memory.Address = 2;
        memory.WriteEnable = true; // a pending write intent should not block the read phase
        memory.PreClockUpdate();

        Assert.AreEqual(15, memory.ReadData);
    }

    [Test]
    public void PreClockUpdate_BeforeClock_DoesNotSeeSameCycleWrite()
    {
        // Two-phase design: a write scheduled for this cycle via Clock() must not
        // be visible to a PreClockUpdate() read that happens before Clock() runs.
        var memory = new DataInstMemory();
        memory.LoadWord(1, 100);

        memory.Address = 1;
        memory.WriteData = 200;
        memory.WriteEnable = true;
        memory.PreClockUpdate(); // reads the OLD value

        Assert.AreEqual(100, memory.ReadData);

        memory.Clock(); // the write commits now
        memory.PreClockUpdate(); // the next read sees the NEW value

        Assert.AreEqual(200, memory.ReadData);
    }

    [Test]
    public void WriteThenReadDifferentAddress_DoesNotAffectOtherAddresses()
    {
        var memory = new DataInstMemory { Address = 5, WriteData = 9, WriteEnable = true };
        memory.Clock();

        memory.Address = 6;
        memory.PreClockUpdate();

        Assert.AreEqual(0, memory.ReadData);
    }
}
