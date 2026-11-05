using System.Collections.Generic;
using NUnit.Framework;

public class AluTests
{
    [Test]
    public void Add_ReturnsSum()
    {
        Assert.AreEqual(5, Alu.Calculate(2, 3, AluOperation.ADD));
    }

    [Test]
    public void Sub_WhenOperandsEqual_ReturnsZero()
    {
        // the zero flag is computed by the caller: Alu.Calculate(...) == 0
        Assert.AreEqual(0, Alu.Calculate(4, 4, AluOperation.SUB));
    }

    [Test]
    public void Sub_ReturnsDifference()
    {
        Assert.AreEqual(-3, Alu.Calculate(2, 5, AluOperation.SUB));
    }

    [Test]
    public void And_ReturnsBitwiseAnd()
    {
        Assert.AreEqual(0b1000, Alu.Calculate(0b1100, 0b1010, AluOperation.AND));
    }

    [Test]
    public void Or_ReturnsBitwiseOr()
    {
        Assert.AreEqual(0b1110, Alu.Calculate(0b1100, 0b0010, AluOperation.OR));
    }

    // The numeric value of AluOperation is the button index in the ALU panel.
    [TestCase(0, 2, 3, 5)]              // ADD
    [TestCase(1, 5, 3, 2)]              // SUB
    [TestCase(2, 0b110, 0b011, 0b010)]  // AND
    [TestCase(3, 0b110, 0b011, 0b111)]  // OR
    public void ButtonIndex_MapsToOperation(int buttonIndex, int a, int b, int expected)
    {
        Assert.AreEqual(expected, Alu.Calculate(a, b, (AluOperation)buttonIndex));
    }

    [Test]
    public void UnknownOperation_ReturnsZero()
    {
        Assert.AreEqual(0, Alu.Calculate(2, 3, (AluOperation)99));
    }

    // ---------------------------------------------------------------- ALU as a model (control signal + event)

    [Test]
    public void Model_DefaultOperation_IsAdd()
    {
        Assert.AreEqual(AluOperation.ADD, new Alu().Operation);
    }

    [Test]
    public void Model_Compute_UsesCurrentOperation()
    {
        var alu = new Alu { Operation = AluOperation.SUB };

        Assert.AreEqual(2, alu.Compute(5, 3));

        alu.Operation = AluOperation.OR;
        Assert.AreEqual(0b111, alu.Compute(0b110, 0b011));
    }

    [Test]
    public void Model_SettingOperation_RaisesEvent_EvenForTheSameValue()
    {
        // the view relies on this: restoring the same operation from history must still refresh the banner
        var alu = new Alu();
        var received = new List<AluOperation>();
        alu.OperationChanged += received.Add;

        alu.Operation = AluOperation.AND;
        alu.Operation = AluOperation.AND;

        CollectionAssert.AreEqual(new[] { AluOperation.AND, AluOperation.AND }, received);
    }
}
