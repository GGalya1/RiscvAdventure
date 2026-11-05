using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelTwoExtendedState
{
    public int RegisterAValue;
    public bool RegisterAwe;
    public int RegisterBValue;
    public bool RegisterBwe;
    public int OutputValue;
    public bool OutputWe;
    public int AluOperation;
}

[System.Serializable]
public class LevelTwoExtendedBusSegments: IBusSegmentProvider
{
    [Tooltip("SrcA register -> ALU input A")]
    public LineRenderer srcAToAlu;
    [Tooltip("SrcB register → ALU input B")]
    public LineRenderer srcBToAlu;
    [Tooltip("ALU result → Output register")]
    public LineRenderer aluToOutput;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToAlu);
        c.RegisterSegment(srcBToAlu);
        c.RegisterSegment(aluToOutput);
    }
}

public class LevelTwoExtended : BaseLevelRegisseur<LevelTwoExtendedState, LevelTwoExtendedBusSegments>
{
    [FormerlySerializedAs("_aluVizualizer")]
    [Header("Level 2 (Extender) Specific Components")]
    [SerializeField] private AluVisualiser aluVisualizer;
    [FormerlySerializedAs("registerSrcAVizualizer")] [FormerlySerializedAs("_registerSrcAVizualizer")] [SerializeField] private RegisterVisualizer registerSrcAVisualizer;
    [FormerlySerializedAs("registerSrcBVizualizer")] [FormerlySerializedAs("_registerSrcBVizualizer")] [SerializeField] private RegisterVisualizer registerSrcBVisualizer;
    [FormerlySerializedAs("registerOutputVizualizer")] [FormerlySerializedAs("_registerOutputVizualizer")] [SerializeField] private RegisterVisualizer registerOutputVisualizer;

    [SerializeField] private int srcAValue;
    [SerializeField] private int srcBValue;

    #region CACHED UI REFERENCES
    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;
    private InfoPanelUI _infoOutputRegister;
    #endregion

    private Register _srcA;
    private Register _srcB;
    private Register _output;

    private int _currentBus; // [0, 1]
    
    protected override void OnLevelStart()
    {
        _srcA = new Register(srcAValue);
        _srcB = new Register(srcBValue);
        _output = new Register();

        Clock.Add(_srcA, _srcB, _output);

        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;
        _infoOutputRegister = registerOutputVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override void ApplyState(LevelTwoExtendedState s)
    {
        _srcA.Reset(s.RegisterAValue);
        _srcA.WriteEnable = s.RegisterAwe;


        _srcB.Reset(s.RegisterBValue);
        _srcB.WriteEnable = s.RegisterBwe;

        _output.Reset(s.OutputValue);
        _output.WriteEnable = s.OutputWe;

        aluVisualizer.ChooseAluOperation(s.AluOperation);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerOutputVisualizer.TriggerBlink();
    }

    protected override bool CheckWinCondition()
    {
        return (_output.Output == RightAnswerValue);
    }

    protected override LevelTwoExtendedState GetCurrentState()
    {
        return new LevelTwoExtendedState
        {
            RegisterAValue = _srcA.Output,
            RegisterAwe = _srcA.WriteEnable,
            RegisterBValue = _srcB.Output,
            RegisterBwe = _srcB.WriteEnable,
            OutputValue = _output.Output,
            OutputWe = _output.WriteEnable,
            AluOperation = aluVisualizer.CurrentAluOperation,
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _output.WriteEnable = registerOutputVisualizer.isWriteEnabled;

        _output.Input = Alu.Calculate(_srcA.Output, _srcB.Output, aluVisualizer.CurrentAluOperation);

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            yield return Bus.WaveBack((buses.aluToOutput, _output.Input));
            yield return Bus.WaveBack((buses.srcAToAlu, _srcA.Output), (buses.srcBToAlu, _srcB.Output));

            _currentBus--;
        }
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            yield return Bus.Wave((buses.srcAToAlu, _srcA.Output), (buses.srcBToAlu, _srcB.Output));
            yield return Bus.Wave((buses.aluToOutput, Alu.Calculate(_srcA.Output, _srcB.Output, aluVisualizer.CurrentAluOperation)));

            _currentBus++;
        }
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
}
