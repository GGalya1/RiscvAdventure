using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelZeroState
{
    public RegisterState SrcA;
    public RegisterState SrcB;
    public RegisterState Output;
}

[Serializable]
public class LevelZeroBusSegments
{
    [Tooltip("SrcA register -> SrcB register")]
    public LineRenderer srcAToSrcB;

    [Tooltip("SrcB register -> Output register")]
    public LineRenderer srcBToOutput;
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
        _srcA.Restore(s.SrcA);
        _srcB.Restore(s.SrcB);
        _output.Restore(s.Output);
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override LevelZeroState GetCurrentState() => new()
    {
        SrcA = _srcA.Save(),
        SrcB = _srcB.Save(),
        Output = _output.Save()
    };

    protected override void HandleClockUpdate()
    {
        _srcB.Input = _srcA.Output;
        _output.Input = _srcB.Output;

        Clock.Tick();
    }

    // Only the first two ticks move data along the chain.
    protected override IEnumerator RunBusVisualizations()
    {
        switch (TickCounter)
        {
            case 0: yield return Bus.Wave((buses.srcAToSrcB, _srcA.Output)); break;
            case 1: yield return Bus.Wave((buses.srcBToOutput, _srcB.Output)); break;
        }
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        switch (TickCounter)
        {
            case 1: yield return Bus.WaveBack((buses.srcBToOutput, _output.Input)); break;
            case 0: yield return Bus.WaveBack((buses.srcAToSrcB, _srcB.Input)); break;
        }
    }
}