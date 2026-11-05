using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelZeroState
{
    public int RegisterAValue;
    public int RegisterBValue;
    public int OutputRegisterValue;

    public bool RegisterAwe;
    public bool RegisterBwe;
    public bool OutputRegisterWe;
}

[Serializable]
public class LevelZeroBusSegments: IBusSegmentProvider
{
    [Tooltip("SrcA register -> SrcB register")]
    public LineRenderer srcAToSrcB;

    [Tooltip("SrcB register -> Output register")]
    public LineRenderer srcBToOutput;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToSrcB);
        c.RegisterSegment(srcBToOutput);
    }
}

public class LevelZeroRegisseur : BaseLevelRegisseur<LevelZeroState, LevelZeroBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")] [Header("Level 0 Specific Components")] [SerializeField]
    private RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField]
    private RegisterVisualizer registerSrcBVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    private RegisterVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("_srcAValue")] [SerializeField]
    private int srcAValue;

    [FormerlySerializedAs("_srcBValue")] [SerializeField]
    private int srcBValue;

    private int _currentBus; // [0, 2]
    private Register _output;

    // Intern components for computations
    private Register _srcA;
    private Register _srcB;

    protected override void OnLevelStart()
    {
        _srcA = new Register(srcAValue);
        _srcB = new Register(srcBValue);
        _output = new Register();

        Clock.Add(_srcA, _srcB, _output);

        Show(
            registerSrcAVisualizer.Bind(_srcA, "Register 1"),
            registerSrcBVisualizer.Bind(_srcB, "Register 2"),
            registerOutputVisualizer.Bind(_output, "Register 3"));
    }

    protected override void ApplyState(LevelZeroState s)
    {
        _srcA.Reset(s.RegisterAValue);
        _srcB.Reset(s.RegisterBValue);
        _output.Reset(s.OutputRegisterValue);
        _srcA.WriteEnable = s.RegisterAwe;
        _srcB.WriteEnable = s.RegisterBwe;
        _output.WriteEnable = s.OutputRegisterWe;
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override LevelZeroState GetCurrentState()
    {
        return new LevelZeroState
        {
            RegisterAValue = _srcA.Output,
            RegisterBValue = _srcB.Output,
            OutputRegisterValue = _output.Output,
            RegisterAwe = _srcA.WriteEnable,
            RegisterBwe = _srcB.WriteEnable,
            OutputRegisterWe = _output.WriteEnable
        };
    }

    protected override void HandleClockUpdate()
    {
        _srcB.Input = _srcA.Output;
        _output.Input = _srcB.Output;

        Clock.Tick();
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus == 0)
            yield return Bus.Wave((buses.srcAToSrcB, _srcA.Output));
        else if (_currentBus == 1)
            yield return Bus.Wave((buses.srcBToOutput, _srcB.Output));

        _currentBus++;
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus == 2)
            yield return Bus.WaveBack((buses.srcBToOutput, _output.Input));
        else if (_currentBus == 1)
            yield return Bus.WaveBack((buses.srcAToSrcB, _srcB.Input));

        _currentBus--;
    }
}