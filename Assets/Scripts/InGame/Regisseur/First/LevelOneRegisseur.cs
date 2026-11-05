using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelOneState
{
    public int RegisterAValue;
    public int RegisterBValue;
    public int OutputRegisterValue;

    public bool RegisterAwe;
    public bool RegisterBwe;
    public bool OutputRegisterWe;

    public int CurrentChosenMuxPath;
}

[Serializable]
public class LevelOneBusSegments: IBusSegmentProvider
{
    [Tooltip("SrcA register -> MUX input [0]")]
    public LineRenderer srcAToMux;

    [Tooltip("SrcB register -> MUX input [1]")]
    public LineRenderer srcBToMux;

    [Tooltip("MUX output -> Output register")]
    public LineRenderer muxToOutput;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToMux);
        c.RegisterSegment(srcBToMux);
        c.RegisterSegment(muxToOutput);
    }
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

    private int _currentBus; // [0, 1]
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

    protected override LevelOneState GetCurrentState()
    {
        return new LevelOneState
        {
            RegisterAValue = _srcA.Output,
            RegisterBValue = _srcB.Output,
            OutputRegisterValue = _output.Output,
            CurrentChosenMuxPath = _mux.SelectedInput,
            RegisterAwe = _srcA.WriteEnable,
            RegisterBwe = _srcB.WriteEnable,
            OutputRegisterWe = _output.WriteEnable
        };
    }

    protected override void ApplyState(LevelOneState s)
    {
        _srcA.Reset(s.RegisterAValue);
        _srcB.Reset(s.RegisterBValue);
        _output.Reset(s.OutputRegisterValue);
        _srcA.WriteEnable = s.RegisterAwe;
        _srcB.WriteEnable = s.RegisterBwe;
        _output.WriteEnable = s.OutputRegisterWe;

        _mux.SelectedInput = s.CurrentChosenMuxPath;
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
        if (_currentBus == 0)
        {
            yield return Bus.Wave((buses.srcAToMux, _srcA.Output), (buses.srcBToMux, _srcB.Output));

            if (_mux.HasSelection)
            {
                var propagationVal = _mux.SelectedInput switch
                {
                    0 => _srcA.Output,
                    1 => _srcB.Output,
                    _ => 0
                };

                yield return Bus.Wave((buses.muxToOutput, propagationVal));
            }

            _currentBus++;
        }
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus == 1)
        {
            if (!_mux.HasSelection)
                yield return Bus.WaveBack((buses.srcAToMux, 4), (buses.srcBToMux, 2));
            else
                yield return Bus.WaveBack((buses.muxToOutput, _output.Input), (buses.srcAToMux, 4), (buses.srcBToMux, 2));

            _currentBus--;
        }
    }
}