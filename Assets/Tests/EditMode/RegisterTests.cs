using NUnit.Framework;

public class RegisterTests
{
    [Test]
    public void Constructor_Default_OutputIsZero()
    {
        var register = new Register();

        Assert.AreEqual(0, register.Output);
    }

    [Test]
    public void Constructor_WithInitialValue_OutputIsInitialValue()
    {
        var register = new Register(42);

        Assert.AreEqual(42, register.Output);
    }

    [Test]
    public void Constructor_WriteEnable_DefaultsToTrue()
    {
        Assert.IsTrue(new Register().WriteEnable);
        Assert.IsFalse(new Register(3, writeEnable: false).WriteEnable);
    }

    [Test]
    public void ClockCycle_WriteEnabled_StoresInputAsNewOutput()
    {
        var register = new Register() { Input = 7, WriteEnable = true };

        register.PreClockUpdate();
        register.Clock();

        Assert.AreEqual(7, register.Output);
    }

    [Test]
    public void ClockCycle_WriteDisabled_KeepsPreviousOutput()
    {
        var register = new Register(3) { Input = 99, WriteEnable = false };

        register.PreClockUpdate();
        register.Clock();

        Assert.AreEqual(3, register.Output);
    }

    [Test]
    public void SettingInput_WithoutClocking_DoesNotChangeOutput()
    {
        var register = new Register(5) { WriteEnable = true };

        register.Input = 123;

        Assert.AreEqual(5, register.Output);
    }

    [Test]
    public void Clock_WithoutPriorPreClockUpdate_DoesNotPickUpNewInput()
    {
        // The register is two-phase: Clock() only latches whatever PreClockUpdate()
        // last buffered. Changing Input/WriteEnable after PreClockUpdate (but before
        // Clock) must have no effect until the next PreClockUpdate() runs.
        var register = new Register(1);
        register.WriteEnable = false;
        register.PreClockUpdate(); // buffers the current Output (1), since WriteEnable is false

        register.Input = 55;
        register.WriteEnable = true; // changed too late, after the setup phase already ran
        register.Clock();

        Assert.AreEqual(1, register.Output);
    }

    [Test]
    public void MultipleClockCycles_UpdateSequentially()
    {
        var register = new Register();

        register.Input = 10;
        register.WriteEnable = true;
        register.PreClockUpdate();
        register.Clock();
        Assert.AreEqual(10, register.Output);

        register.WriteEnable = false;
        register.Input = 999; // should be ignored this cycle
        register.PreClockUpdate();
        register.Clock();
        Assert.AreEqual(10, register.Output);

        register.Input = 20;
        register.WriteEnable = true;
        register.PreClockUpdate();
        register.Clock();
        Assert.AreEqual(20, register.Output);
    }

    [Test]
    public void Reset_ImmediatelyOverridesOutput()
    {
        var register = new Register(1) { Input = 8, WriteEnable = true };
        register.PreClockUpdate();
        register.Clock();

        register.Reset(100);

        Assert.AreEqual(100, register.Output);
    }

    [Test]
    public void Reset_ThenClockWithoutWrite_KeepsResetValue()
    {
        var register = new Register(1);
        register.Reset(50);
        register.WriteEnable = false;

        register.PreClockUpdate();
        register.Clock();

        Assert.AreEqual(50, register.Output);
    }
}
