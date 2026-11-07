using System.Collections.Generic;
using NUnit.Framework;

public class MultiplexerTests
{
    // Select2To1(a, b, true) used to return a: that is input 0 now
    [Test]
    public void Select_Input0_ReturnsFirstInput()
    {
        Assert.AreEqual(1, Multiplexer.Select(0, 1, 2));
    }

    [Test]
    public void Select_Input1_ReturnsSecondInput()
    {
        Assert.AreEqual(2, Multiplexer.Select(1, 1, 2));
    }

    [TestCase(0, 10)]
    [TestCase(1, 20)]
    [TestCase(2, 30)]
    public void Select_ReturnsInputAtControlIndex(int control, int expected)
    {
        Assert.AreEqual(expected, Multiplexer.Select(control, 10, 20, 30));
    }

    [Test]
    public void Select_ThirdInputOmitted_IsZero()
    {
        Assert.AreEqual(0, Multiplexer.Select(2, 10, 20));
    }

    [TestCase(Multiplexer.NoInput)]
    [TestCase(3)]
    public void Select_NoOrInvalidSelection_ReturnsZero(int control)
    {
        Assert.AreEqual(0, Multiplexer.Select(control, 10, 20, 30));
    }

    // ---------------------------------------------------------------- Multiplexer as a model (control signal + event)

    [Test]
    public void Model_Default_HasNoSelectionAndOutputsZero()
    {
        var mux = new Multiplexer();

        Assert.AreEqual(Multiplexer.NoInput, mux.SelectedInput);
        Assert.IsFalse(mux.HasSelection);
        Assert.AreEqual(0, mux.Route(10, 20, 30));
    }

    [Test]
    public void Model_Route_UsesSelectedInput()
    {
        var mux = new Multiplexer { SelectedInput = 1 };

        Assert.IsTrue(mux.HasSelection);
        Assert.AreEqual(20, mux.Route(10, 20, 30));
    }

    [Test]
    public void Model_ResetToNoInput_ClearsSelection()
    {
        var mux = new Multiplexer { SelectedInput = 2 };

        mux.SelectedInput = Multiplexer.NoInput;

        Assert.IsFalse(mux.HasSelection);
        Assert.AreEqual(0, mux.Route(10, 20, 30));
    }

    [Test]
    public void Model_SettingSelection_RaisesEvent()
    {
        var mux = new Multiplexer();
        var received = new List<int>();
        mux.SelectionChanged += received.Add;

        mux.SelectedInput = 0;
        mux.SelectedInput = Multiplexer.NoInput;

        CollectionAssert.AreEqual(new[] { 0, Multiplexer.NoInput }, received);
    }
}
