using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelFiveExtendedState
{
    public int RegisterValue;
    public int RegisterOutputValue;

    public bool RegisterWe;
    public bool RegisterOutputWe;

    public ImmediateType ExtenderOperation;
}

[Serializable]
public class LevelFiveExtendedBusSegments: IBusSegmentProvider
{
    [Tooltip("SrcA register (raw instruction word) -> Extend unit")]
    public LineRenderer srcAToExtend;

    [Tooltip("Extend unit output -> Output register")]
    public LineRenderer extendToOutput;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToExtend);
        c.RegisterSegment(extendToOutput);
    }
}

public class LevelFiveExtended : BaseLevelRegisseur<LevelFiveExtendedState, LevelFiveExtendedBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")]
    [Header("Level 5 (Extended) Specific Components")]
    [SerializeField]
    private RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    private RegisterVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("extenderVizualizer")] [FormerlySerializedAs("_extenderVizualizer")] [SerializeField]
    private ExtenderVisualizer extenderVisualizer;

    [FormerlySerializedAs("InputRegisterValue")] [SerializeField]
    private uint inputRegisterValue;

    private Register _output;

    private Register _srcA;

    protected int CurrentBus = 0;

    private readonly Extender _extender = new();

    protected override void OnLevelStart()
    {
        extenderVisualizer.Bind(_extender);

        _srcA = new Register((int)inputRegisterValue);
        _output = new Register();

        Clock.Add(_srcA, _output);

        Show(
            registerSrcAVisualizer.Bind(_srcA, "Register 1", word => RiscVDecoder.CommandBuilder((uint)word)),
            registerOutputVisualizer.Bind(_output, "Register 2"));
    }

    protected override void ApplyState(LevelFiveExtendedState s)
    {
        _srcA.Reset(s.RegisterValue);
        _output.Reset(s.RegisterOutputValue);

        _srcA.WriteEnable = s.RegisterWe;
        _output.WriteEnable = s.RegisterOutputWe;

        _extender.Type = s.ExtenderOperation;
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override LevelFiveExtendedState GetCurrentState()
    {
        return new LevelFiveExtendedState
        {
            RegisterValue = _srcA.Output,
            RegisterOutputValue = _output.Output,

            RegisterWe = _srcA.WriteEnable,
            RegisterOutputWe = _output.WriteEnable,

            ExtenderOperation = _extender.Type
        };
    }

    protected override void HandleClockUpdate()
    {
        _output.Input = _extender.Extend(_srcA.Output);

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        yield return Bus.WaveBack((buses.srcAToExtend, _srcA.Output), (buses.extendToOutput, _output.Input));
    }

    protected override IEnumerator RunBusVisualizations()
    {
        yield return Bus.Wave((buses.srcAToExtend, _srcA.Output), (buses.extendToOutput, _output.Output));
    }
}