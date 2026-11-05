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

    protected override void OnLevelStart()
    {
        // Initialization of logical components
        _srcA = new Register(4);
        _srcB = new Register(2);
        _output = new Register();

        Clock.Add(_srcA, _srcB, _output);

        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;
        _infoOutputRegister = registerOutputVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerOutputVisualizer.TriggerBlink();
    }

    protected override LevelOneState GetCurrentState()
    {
        return new LevelOneState
        {
            RegisterAValue = _srcA.Output,
            RegisterBValue = _srcB.Output,
            OutputRegisterValue = _output.Output,
            CurrentChosenMuxPath = multiplexerVisualizer.CurrentChosenMuxPath,
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

        ApplyMuxState(s.CurrentChosenMuxPath, multiplexerVisualizer);
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register 1", _srcA.Output);
        _infoSrcBRegister.Display("Register 2", _srcB.Output);
        _infoOutputRegister.Display("Register 3", _output.Output);

        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerOutputVisualizer.ForceUpdateWriteEnableVisualization(_output.WriteEnable);
    }

    protected override void HandleClockUpdate()
    {
        var path = multiplexerVisualizer.CurrentChosenMuxPath;
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

        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _output.WriteEnable = registerOutputVisualizer.isWriteEnabled;

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

            if (multiplexerVisualizer.CurrentChosenMuxPath != -1)
            {
                var propagationVal = multiplexerVisualizer.CurrentChosenMuxPath switch
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
            if (multiplexerVisualizer.CurrentChosenMuxPath == -1)
                yield return Bus.WaveBack((buses.srcAToMux, 4), (buses.srcBToMux, 2));
            else
                yield return Bus.WaveBack((buses.muxToOutput, _output.Input), (buses.srcAToMux, 4), (buses.srcBToMux, 2));

            _currentBus--;
        }
    }

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;
    private InfoPanelUI _infoOutputRegister;

    #endregion
}