using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelSevenState
{
    public int RegisterAValue;
    public int RegisterBValue;
    public int RegisterOutputValue;

    public bool RegisterAwe;
    public bool RegisterBwe;
    public bool RegisterOutputWe;
    public bool RegisterFileWe;

    public AluOperation AluOperation;
}

[Serializable]
public class LevelSevenBusSegments: IBusSegmentProvider
{
    [Tooltip("SrcA register (rs1 address) -> Register File A1")]
    public LineRenderer srcAToRegFileA1;

    [Tooltip("SrcB register (rs2 address) -> Register File A2")]
    public LineRenderer srcBToRegFileA2;

    [Tooltip("Register File RD1 -> ALU input A")]
    public LineRenderer rd1ToAlu;

    [Tooltip("Register File RD2 -> ALU input B")]
    public LineRenderer rd2ToAlu;

    [Tooltip("ALU result -> Output register")]
    public LineRenderer aluToOutput;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToRegFileA1);
        c.RegisterSegment(srcBToRegFileA2);
        c.RegisterSegment(rd1ToAlu);
        c.RegisterSegment(rd2ToAlu);
        c.RegisterSegment(aluToOutput);
    }
}

public class LevelSevenRegisseur : BaseLevelRegisseur<LevelSevenState, LevelSevenBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")] [Header("Level 7 Specific Components")] [SerializeField]
    protected RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField]
    protected RegisterVisualizer registerSrcBVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    protected RegisterVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("_registerFileVisualizer")] [SerializeField]
    protected RegisterFileVisualizer registerFileVisualizer;

    [FormerlySerializedAs("_aluVizualizer")] [SerializeField]
    protected AluVisualiser aluVisualizer;


    private int _currentBus; // [0, 2]
    private Register _output;
    private RegisterFile _registerFile;

    // Intern components for computations
    private Register _srcA;
    private Register _srcB;

    protected override int RightAnswerValue => 42;

    private readonly Alu _alu = new();

    protected override void OnLevelStart()
    {
        aluVisualizer.Bind(_alu);

        // Initialization of logical components
        _srcA = new Register(6);
        _srcB = new Register(7);
        _output = new Register();

        _registerFile = new RegisterFile(new[]
        {
            0, 1, 39, 43, 5, 6, 2,
            40, 1, 39, 13, 56, 64, 20,
            50, 51, 0, 12, 53, 65, 29,
            60, 61, 0, 1, 54, 0, 28,
            70, 30, 31, 0
        });

        Clock.Add(_srcA, _srcB, _output, _registerFile);

        // Caching of UI panels for visualizers
        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;
        _infoOutputRegister = registerOutputVisualizer.UIRegisterPanel;


        UpdateVisualizers();
        UpdateRegisterFileVisualisation();
    }

    protected override void ApplyState(LevelSevenState s)
    {
        _srcA.Reset(s.RegisterAValue);
        _srcB.Reset(s.RegisterBValue);
        _output.Reset(s.RegisterOutputValue);

        _srcA.WriteEnable = s.RegisterAwe;
        _srcB.WriteEnable = s.RegisterBwe;
        _output.WriteEnable = s.RegisterOutputWe;
        _registerFile.WriteEnable = s.RegisterFileWe;

        _alu.Operation = s.AluOperation;
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerOutputVisualizer.TriggerBlink();

        registerFileVisualizer.TriggerBlink();
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override LevelSevenState GetCurrentState()
    {
        return new LevelSevenState
        {
            RegisterAValue = _srcA.Output,
            RegisterBValue = _srcB.Output,
            RegisterOutputValue = _output.Output,

            RegisterAwe = _srcA.WriteEnable,
            RegisterBwe = _srcB.WriteEnable,
            RegisterOutputWe = _output.WriteEnable,
            RegisterFileWe = _registerFile.WriteEnable,

            AluOperation = _alu.Operation
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _output.WriteEnable = registerOutputVisualizer.isWriteEnabled;
        _registerFile.WriteEnable = registerFileVisualizer.isWriteEnabled;

        // implementation
        _registerFile.ReadAddress1 = _srcA.Output;
        _registerFile.ReadAddress2 = _srcB.Output;
        _output.Input = _alu.Compute(_registerFile.ReadData1, _registerFile.ReadData2);

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            var before = StateAt(TickCounter);

            yield return Bus.WaveBack((buses.aluToOutput, _output.Input));
            yield return Bus.WaveBack(
                (buses.rd1ToAlu, _registerFile.Registers[before.RegisterAValue]),
                (buses.rd2ToAlu, _registerFile.Registers[before.RegisterBValue]));
            yield return Bus.WaveBack((buses.srcAToRegFileA1, before.RegisterAValue), (buses.srcBToRegFileA2, before.RegisterBValue));

            _currentBus--;
        }
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var rd1 = _registerFile.ReadData1;
            var rd2 = _registerFile.ReadData2;

            yield return Bus.Wave((buses.srcAToRegFileA1, _srcA.Output), (buses.srcBToRegFileA2, _srcB.Output));
            yield return Bus.Wave((buses.rd1ToAlu, rd1), (buses.rd2ToAlu, rd2));
            yield return Bus.Wave((buses.aluToOutput, _alu.Compute(rd1, rd2)));

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
        registerFileVisualizer.ForceUpdateWriteEnableVisualization(_registerFile.WriteEnable);
    }

    private void UpdateRegisterFileVisualisation()
    {
        registerFileVisualizer.UIRegisterPanel.Display(_registerFile.Registers);
    }

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;
    private InfoPanelUI _infoOutputRegister;

    #endregion
}