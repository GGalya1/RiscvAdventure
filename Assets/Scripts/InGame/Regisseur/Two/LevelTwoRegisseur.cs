using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelTwoState
{
    public int RegisterAValue;
    public bool RegisterAwe;
    public int AluOperation;
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

    #region CACHED UI REFERENCES
    private InfoPanelUI _infoSrcARegister;
    #endregion

    private Register _srcA;

    protected override int RightAnswerValue => 8;

    private int _currentBus; // [0, 1]

    protected override void OnLevelStart()
    {
        _srcA = new Register(1)
        {
            WriteEnable = true
        };
        // second argument must be 4

        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override void ApplyState(LevelTwoState s)
    {
        _srcA.Reset(s.RegisterAValue);
        _srcA.WriteEnable = s.RegisterAwe;

        aluVisualizer.ChooseAluOperation(s.AluOperation);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        numberBlinker.Trigger();
    }

    protected override bool CheckWinCondition()
    {
        return (_srcA.Output == RightAnswerValue);
    }

    protected override LevelTwoState GetCurrentState()
    {
        return new LevelTwoState
        {
            RegisterAValue = _srcA.Output,
            RegisterAwe = _srcA.WriteEnable,
            AluOperation = aluVisualizer.CurrentAluOperation,
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;

        _srcA.Input = Alu.Calculate(_srcA.Output, 4, aluVisualizer.CurrentAluOperation);
        _srcA.PreClockUpdate();
        _srcA.Clock();
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register 1", _srcA.Output);

        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
    }

    #region busVizualisation
    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus is >= 0 and < 5)
        {
            yield return Bus.Wave((buses.pcToAdder, _srcA.Output), (buses.constFourToAdder, 4));
            yield return Bus.Wave((buses.adderToPC, Alu.Calculate(_srcA.Output, 4, aluVisualizer.CurrentAluOperation)));

            _currentBus++;
        }
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus is >= 1 and <= 5)
        {
            yield return Bus.WaveBack((buses.adderToPC, _srcA.Input));
            yield return Bus.WaveBack((buses.pcToAdder, StateAt(TickCounter).RegisterAValue), (buses.constFourToAdder, 4));

            _currentBus--;
        }
    }
    #endregion
}
