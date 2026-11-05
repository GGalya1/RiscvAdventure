using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelTwoState
{
    public RegisterState SrcA;
    public AluOperation AluOperation;
}

[System.Serializable]
public class LevelTwoBusSegments: IBusSegmentProvider
{
    [Tooltip("PC register output -> ALU input A")]
    public LineRenderer pcToAdder;
    [Tooltip("Constant 4 -> ALU input B")]
    public LineRenderer constFourToAdder;
    [Tooltip("ALU/adder result -> PC register input")]
    public LineRenderer adderToPC;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(pcToAdder);
        c.RegisterSegment(constFourToAdder);
        c.RegisterSegment(adderToPC);
    }
}

public class LevelTwoRegisseur : BaseLevelRegisseur<LevelTwoState, LevelTwoBusSegments>
{
    [FormerlySerializedAs("_aluVizualizer")]
    [Header("Level 2 Specific Components")]
    [SerializeField] private AluVisualiser aluVisualizer;
    [FormerlySerializedAs("_registerSrcAVisualizer")] [SerializeField] private RegisterVisualizer registerSrcAVisualizer;
    [FormerlySerializedAs("_numberBlinker")] [SerializeField] private Blinker numberBlinker;

    private Register _srcA;

    protected override int RightAnswerValue => 8;

    private int _currentBus; // [0, 1]

    private readonly Alu _alu = new();

    protected override void OnLevelStart()
    {
        aluVisualizer.Bind(_alu);

        _srcA = new Register(1);
        // second argument must be 4

        Clock.Add(_srcA);

        Show(registerSrcAVisualizer.Bind(_srcA, "Register 1"));
    }

    protected override void ApplyState(LevelTwoState s)
    {
        _srcA.Restore(s.SrcA);
        _alu.Operation = s.AluOperation;
    }

    protected override void BlinkClockedComponents()
    {
        base.BlinkClockedComponents();
        numberBlinker.Trigger();
    }

    protected override bool CheckWinCondition()
    {
        return (_srcA.Output == RightAnswerValue);
    }

    protected override LevelTwoState GetCurrentState() => new()
    {
        SrcA = _srcA.Save(),
        AluOperation = _alu.Operation
    };

    protected override void HandleClockUpdate()
    {
        _srcA.Input = _alu.Compute(_srcA.Output, 4);
        Clock.Tick();
    }

    #region busVizualisation
    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus is >= 0 and < 5)
        {
            yield return Bus.Wave((buses.pcToAdder, _srcA.Output), (buses.constFourToAdder, 4));
            yield return Bus.Wave((buses.adderToPC, _alu.Compute(_srcA.Output, 4)));

            _currentBus++;
        }
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus is >= 1 and <= 5)
        {
            yield return Bus.WaveBack((buses.adderToPC, _srcA.Input));
            yield return Bus.WaveBack((buses.pcToAdder, StateAt(TickCounter).SrcA.Value), (buses.constFourToAdder, 4));

            _currentBus--;
        }
    }
    #endregion
}
