using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelOneState
{
    public RegisterState SrcA;
    public RegisterState SrcB;
    public RegisterState Output;
    public int MuxInput;
}

[Serializable]
public class LevelOneBusSegments
{
    [Tooltip("SrcA register -> MUX input [0]")]
    public LineRenderer srcAToMux;

    [Tooltip("SrcB register -> MUX input [1]")]
    public LineRenderer srcBToMux;

    [Tooltip("MUX output -> Output register")]
    public LineRenderer muxToOutput;
}

public class LevelOneRegisseur : BaseLevelRegisseur<LevelOneState, LevelOneBusSegments>
{
    [FormerlySerializedAs("_multiplexerVisualizer")] [Header("Level 1 Specific Components")] [SerializeField]
    private MultiplexerVisualizer multiplexerVisualizer;

    [FormerlySerializedAs("_registerSrcAVisualizer")] [SerializeField]
    private RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField]
    private RegisterVisualizer registerSrcBVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    private RegisterVisualizer registerOutputVisualizer;

    // protected override int RightAnswerValue => 4;

    private Register _output;

    // Intern components for computations
    private Register _srcA;
    private Register _srcB;

    private readonly Multiplexer _mux = new();

    protected override void OnLevelStart()
    {
        multiplexerVisualizer.Bind(_mux);

        // Initialization of logical components
        _srcA = new Register(4);
        _srcB = new Register(2);
        _output = new Register();

        Clock.Add(_srcA, _srcB, _output);

        Show(
            registerSrcAVisualizer.Bind(_srcA, "Register 1"),
            registerSrcBVisualizer.Bind(_srcB, "Register 2"),
            registerOutputVisualizer.Bind(_output, "Register 3"));
    }

    protected override LevelOneState GetCurrentState() => new()
    {
        SrcA = _srcA.Save(),
        SrcB = _srcB.Save(),
        Output = _output.Save(),
        MuxInput = _mux.SelectedInput
    };

    protected override void ApplyState(LevelOneState s)
    {
        _srcA.Restore(s.SrcA);
        _srcB.Restore(s.SrcB);
        _output.Restore(s.Output);
        _mux.SelectedInput = s.MuxInput;
    }

    protected override void HandleClockUpdate()
    {
        var path = _mux.SelectedInput;
        var res = 0;

        switch (path)
        {
            case -1:
                CustomLog.LogEditorError("Multiplexer path not selected (-1). Data will be lost.");
                break;
            case >= 0 and <= 1:
                res = Multiplexer.Select(path, _srcA.Output, _srcB.Output);
                break;
            default:
                CustomLog.LogEditorError($"Multiplexer path {path} is an invalid value!");
                break;
        }

        _output.Input = res;
        Clock.Tick();
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (TickCounter != 0) yield break;

        yield return Bus.Wave((buses.srcAToMux, _srcA.Output), (buses.srcBToMux, _srcB.Output));

        if (_mux.HasSelection)
            yield return Bus.Wave((buses.muxToOutput, _mux.Route(_srcA.Output, _srcB.Output)));
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (TickCounter != 0) yield break;

        var before = StateAt(TickCounter);
        if (_mux.HasSelection)
        {
            yield return Bus.WaveBack(
                (buses.muxToOutput, _output.Input),
                (buses.srcAToMux, before.SrcA.Value),
                (buses.srcBToMux, before.SrcB.Value));
        }
        else
        {
            yield return Bus.WaveBack((buses.srcAToMux, before.SrcA.Value), (buses.srcBToMux, before.SrcB.Value));
        }
    }
}