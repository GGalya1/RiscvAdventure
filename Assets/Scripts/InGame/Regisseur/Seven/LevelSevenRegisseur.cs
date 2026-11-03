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

    public int AluOperation;
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

    protected override void OnLevelStart()
    {
        // Initialization of logical components
        _srcA = new Register(6)
        {
            WriteEnable = true
        };
        _srcB = new Register(7)
        {
            WriteEnable = true
        };
        _output = new Register
        {
            WriteEnable = true
        };

        _registerFile = new RegisterFile
        {
            RegisterWriteEnable = true
        };
        _registerFile.InitializeRegisters(new[]
        {
            0, 1, 39, 43, 5, 6, 2,
            40, 1, 39, 13, 56, 64, 20,
            50, 51, 0, 12, 53, 65, 29,
            60, 61, 0, 1, 54, 0, 28,
            70, 30, 31, 0
        });

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
        _registerFile.RegisterWriteEnable = s.RegisterFileWe;

        aluVisualizer.ChooseAluOperation(s.AluOperation);
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
            RegisterFileWe = _registerFile.RegisterWriteEnable,

            AluOperation = aluVisualizer.CurrentAluOperation
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _output.WriteEnable = registerOutputVisualizer.isWriteEnabled;
        _registerFile.RegisterWriteEnable = registerFileVisualizer.isWriteEnabled;

        // implementation
        _registerFile.ReadAdress1 = _srcA.Output;
        _registerFile.ReadAdress2 = _srcB.Output;
        _output.Input = Alu.Calculate(_registerFile.ReadData1, _registerFile.ReadData2,
            aluVisualizer.CurrentAluOperation);

        _srcA.PreClockUpdate();
        _srcB.PreClockUpdate();
        _output.PreClockUpdate();
        _registerFile.PreClockUpdate();


        // Only if WriteEnable = true, call Clock
        _srcA.Clock();
        _srcB.Clock();
        _output.Clock();
        _registerFile.Clock();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            busController.StartBusSignal(buses.aluToOutput, _output.Input, true);
            yield return WaitNoSignals;


            busController.StartBusSignal(buses.rd1ToAlu,
                _registerFile.Registers[StateAt(TickCounter).RegisterAValue], true);
            busController.StartBusSignal(buses.rd2ToAlu,
                _registerFile.Registers[StateAt(TickCounter).RegisterBValue], true);
            yield return WaitNoSignals;

            busController.StartBusSignal(buses.srcAToRegFileA1, StateAt(TickCounter).RegisterAValue,
                true);
            busController.StartBusSignal(buses.srcBToRegFileA2, StateAt(TickCounter).RegisterBValue,
                true);


            _currentBus--;
        }

        yield return WaitNoSignals;
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            busController.StartBusSignal(buses.srcAToRegFileA1, _srcA.Output);
            busController.StartBusSignal(buses.srcBToRegFileA2, _srcB.Output);
            yield return WaitNoSignals;

            busController.StartBusSignal(buses.rd1ToAlu, _registerFile.ReadData1);
            busController.StartBusSignal(buses.rd2ToAlu, _registerFile.ReadData2);
            yield return WaitNoSignals;

            busController.StartBusSignal(buses.aluToOutput,
                Alu.Calculate(_registerFile.ReadData1, _registerFile.ReadData2, aluVisualizer.CurrentAluOperation));

            _currentBus++;
        }

        yield return WaitNoSignals;
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register 1", _srcA.Output);
        _infoSrcBRegister.Display("Register 2", _srcB.Output);
        _infoOutputRegister.Display("Register 3", _output.Output);

        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerOutputVisualizer.ForceUpdateWriteEnableVisualization(_output.WriteEnable);
        registerFileVisualizer.ForceUpdateWriteEnableVisualization(_registerFile.RegisterWriteEnable);
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