using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct ExtendedSevenLevelState
{
    public int RegisterSrcAValue;
    public int RegisterSrcBValue;
    public int RegisterA3Value;
    public int RegisterWd3Value;

    public int[] RegisterFieldValue;

    public bool RegisterSrcAwe;
    public bool RegisterSrcBwe;
    public bool RegisterA3We;
    public bool RegisterWd3We;

    public int AluOperation;
}

[System.Serializable]
public class ExtendedLevelSevenBusSegments: IBusSegmentProvider
{
    [Header("Decode - addresses")]
    [Tooltip("SrcA register (rs1 address) -> Register File A1")]
    public LineRenderer srcAToRegFileA1;
    [Tooltip("SrcB register (rs2 address) -> Register File A2")]
    public LineRenderer srcBToRegFileA2;
    [Tooltip("A3 register (rd/destination address) -> Register File A3")]
    public LineRenderer a3ToRegFileA3;

    [Header("Execute")]
    [Tooltip("Register File RD1 -> ALU input A")]
    public LineRenderer rd1ToAlu;
    [Tooltip("Register File RD2 -> ALU input B")]
    public LineRenderer rd2ToAlu;

    [Header("Write Back")]
    [Tooltip("ALU result -> WD3 register")]
    public LineRenderer aluToWd3Reg;
    [Tooltip("WD3 register -> Register File WD3")]
    public LineRenderer wd3RegToRegFile;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToRegFileA1);
        c.RegisterSegment(srcBToRegFileA2);
        c.RegisterSegment(a3ToRegFileA3);
        c.RegisterSegment(rd1ToAlu);
        c.RegisterSegment(rd2ToAlu);
        c.RegisterSegment(aluToWd3Reg);
        c.RegisterSegment(wd3RegToRegFile);
    }
}

public class ExtendedLevelSeven : BaseLevelRegisseur<ExtendedSevenLevelState, ExtendedLevelSevenBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")]
    [Header("Level Seven Components")]
    [SerializeField] protected RegisterVisualizer registerSrcAVisualizer;
    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField] protected RegisterVisualizer registerSrcBVisualizer;
    [FormerlySerializedAs("_registerA3Visualizer")] [SerializeField] protected RegisterVisualizer registerA3Visualizer;
    [FormerlySerializedAs("_registerWD3Visualizer")] [SerializeField] protected RegisterVisualizer registerWd3Visualizer;

    [FormerlySerializedAs("_aluVizualizer")] [SerializeField] protected AluVisualiser aluVisualizer;

    [FormerlySerializedAs("_registerFileVisualizer")] [SerializeField] protected RegisterFileVisualizer registerFileVisualizer;

    #region CACHED UI REFERENCES
    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;
    private InfoPanelUI _infoA3Register;
    private InfoPanelUI _infoWd3Register;
    #endregion

    // Intern components for computations
    private Register _srcA;
    private Register _srcB;
    private Register _a3;
    private Register _wd3;

    private RegisterFile _registerFile;


    private int _currentBus; // [0, 10]

    protected override void OnLevelStart()
    {
        _srcA = new Register(2);
        _srcB = new Register(8);
        _a3 = new Register();
        _wd3 = new Register();

        _registerFile = new RegisterFile(new [] { 0, 1, 39, 43, 5, 6, 8,
                                                     40, 3, 39, 13, 56, 64, 20,
                                                     50, 51, 0, 12, 53, 65, 29,
                                                     60, 61, 0, 25, 54, 0, 28,
                                                     70, 30, 31, 0});

        Clock.Add(_srcA, _srcB, _a3, _wd3, _registerFile);

        // Caching of UI panels for visualizers
        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;
        _infoA3Register = registerA3Visualizer.UIRegisterPanel;
        _infoWd3Register = registerWd3Visualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override void ApplyState(ExtendedSevenLevelState s)
    {
        _srcA.Reset(s.RegisterSrcAValue);
        _srcB.Reset(s.RegisterSrcBValue);
        _a3.Reset(s.RegisterA3Value);
        _wd3.Reset(s.RegisterWd3Value);

        _registerFile.Load(s.RegisterFieldValue);

        _srcA.WriteEnable = s.RegisterSrcAwe;
        _srcB.WriteEnable = s.RegisterSrcBwe;
        _a3.WriteEnable = s.RegisterA3We;
        _wd3.WriteEnable = s.RegisterWd3We;

        aluVisualizer.ChooseAluOperation(s.AluOperation);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerA3Visualizer.TriggerBlink();
        registerWd3Visualizer.TriggerBlink();

        registerFileVisualizer.TriggerBlink();

    }

    protected override bool CheckWinCondition()
    {
        return _registerFile.Registers[0] == 42;
    }

    protected override ExtendedSevenLevelState GetCurrentState()
    {
        return new ExtendedSevenLevelState {
            RegisterSrcAValue = _srcA.Output,
            RegisterSrcBValue = _srcB.Output,
            RegisterA3Value = _a3.Output,
            RegisterWd3Value = _wd3.Output,

            RegisterFieldValue = (int[])_registerFile.Registers.Clone(),

            RegisterSrcAwe = _srcA.WriteEnable,
            RegisterSrcBwe = _srcB.WriteEnable,
            RegisterA3We = _a3.WriteEnable,
            RegisterWd3We = _wd3.WriteEnable,

            AluOperation = aluVisualizer.CurrentAluOperation,
        };
    }

    protected override void HandleClockUpdate()
    {
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _a3.WriteEnable = registerA3Visualizer.isWriteEnabled;
        _wd3.WriteEnable = registerWd3Visualizer.isWriteEnabled;

        _registerFile.WriteEnable = registerFileVisualizer.isWriteEnabled;

        // implementation
        // A1: [19:15] (Register Source 1)
        _registerFile.ReadAddress1 = _srcA.Output;

        // A2: [24:20] (Register Source 2)
        _registerFile.ReadAddress2 = _srcB.Output;

        _registerFile.ReadRegisters();

        // A3: [11:7] (Register Destination / rd)

        var a = _registerFile.ReadData1;
        var b = _registerFile.ReadData2;

        _wd3.Input = Alu.Calculate(a, b, (AluOperation)aluVisualizer.CurrentAluOperation);
        if (TickCounter - 1 >= 0)
        {
            _registerFile.WriteAddress = StateAt(TickCounter - 1).RegisterA3Value;
        }
        _registerFile.WriteData = _wd3.Output;

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            yield return Bus.WaveBack((buses.wd3RegToRegFile, _wd3.Input));
            yield return Bus.WaveBack((buses.aluToWd3Reg, _wd3.Input));
            yield return Bus.WaveBack(
                (buses.rd1ToAlu, DisplayedRegister(_srcA.Output)), (buses.rd2ToAlu, DisplayedRegister(_srcB.Output)));
            yield return Bus.WaveBack(
                (buses.srcAToRegFileA1, _srcA.Output), (buses.srcBToRegFileA2, _srcB.Output), (buses.a3ToRegFileA3, _a3.Output));

            _currentBus--;
        }
    }

    /// <summary> Register value as shown on the RD buses: only r1..r15 are displayed, everything else reads 0. </summary>
    private int DisplayedRegister(int address) =>
        address is > 0 and < 16 ? _registerFile.Registers[address] : 0;

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var a = DisplayedRegister(_srcA.Output);
            var b = DisplayedRegister(_srcB.Output);

            yield return Bus.Wave(
                (buses.srcAToRegFileA1, _srcA.Output), (buses.srcBToRegFileA2, _srcB.Output), (buses.a3ToRegFileA3, _a3.Output));
            yield return Bus.Wave((buses.rd1ToAlu, a), (buses.rd2ToAlu, b));
            yield return Bus.Wave((buses.aluToWd3Reg, Alu.Calculate(a, b, (AluOperation)aluVisualizer.CurrentAluOperation)));
            yield return Bus.Wave((buses.wd3RegToRegFile, _wd3.Output));

            _currentBus++;
        }
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register A1", _srcA.Output);
        _infoSrcBRegister.Display("Register A2", _srcB.Output);
        _infoA3Register.Display("Register A3", _a3.Output);
        _infoWd3Register.Display("Register WD3", _wd3.Output);


        registerFileVisualizer.UIRegisterPanel.Display(_registerFile.Registers);


        // ==============================  WE SECTION  =====================================
        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerA3Visualizer.ForceUpdateWriteEnableVisualization(_a3.WriteEnable);
        registerWd3Visualizer.ForceUpdateWriteEnableVisualization(_wd3.WriteEnable);

        registerFileVisualizer.ForceUpdateWriteEnableVisualization(_registerFile.WriteEnable);
    }
}
