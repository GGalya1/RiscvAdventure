using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct SubExtendedSevenLevelState
{
    public int RegisterSrcAValue;
    public int RegisterImmValue;
    public int RegisterA3Value;
    public int RegisterWd3Value;

    public int[] RegisterFieldValue;

    public bool RegisterSrcAwe;
    public bool RegisterImmWe;
    public bool RegisterA3We;
    public bool RegisterWd3We;

    public int AluOperation;

    public int ExtenderOperation;

    public int MuxPath;
}

[System.Serializable]
public class SubExtendedBusSegments: IBusSegmentProvider
{
    [Header("Decode - addresses")]
    [Tooltip("SrcA register (rs1 address) -> Register File A1")]
    public LineRenderer srcAToRegFileA1;
    [Tooltip("A3 register (rd/destination address) -> Register File A3")]
    public LineRenderer a3ToRegFileA3;
    [Tooltip("ImmValue register -> Extend unit")]
    public LineRenderer immToExtend;

    [Header("Execute")]
    [Tooltip("Register File RD1 -> ALU input A (SrcA)")]
    public LineRenderer rd1ToAlu;
    [Tooltip("Constant 0 -> SrcB MUX input [0] (R-type path)")]
    public LineRenderer zeroToSrcBMux;
    [Tooltip("Extend unit output -> SrcB MUX input [1] (I-type path)")]
    public LineRenderer extToSrcBMux;
    [Tooltip("SrcB MUX output -> ALU input B")]
    public LineRenderer srcBMuxToAlu;

    [Header("Write Back")]
    [Tooltip("ALU result -> WD3 register")]
    public LineRenderer aluToWd3Reg;
    [Tooltip("WD3 register -> Register File WD3")]
    public LineRenderer wd3RegToRegFile;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToRegFileA1);
        c.RegisterSegment(a3ToRegFileA3);
        c.RegisterSegment(immToExtend);
        c.RegisterSegment(rd1ToAlu);
        c.RegisterSegment(zeroToSrcBMux);
        c.RegisterSegment(extToSrcBMux);
        c.RegisterSegment(srcBMuxToAlu);
        c.RegisterSegment(aluToWd3Reg);
        c.RegisterSegment(wd3RegToRegFile);
    }
}

public class SubExtendedRegisseur : BaseLevelRegisseur<SubExtendedSevenLevelState, SubExtendedBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")] [SerializeField] protected RegisterVisualizer registerSrcAVisualizer;
    [FormerlySerializedAs("_registerImmediateVisualizer")] [SerializeField] protected RegisterVisualizer registerImmediateVisualizer;
    [FormerlySerializedAs("_registerA3Visualizer")] [SerializeField] protected RegisterVisualizer registerA3Visualizer;
    [FormerlySerializedAs("_registerWD3Visualizer")] [SerializeField] protected RegisterVisualizer registerWd3Visualizer;

    [FormerlySerializedAs("_aluVizualizer")] [SerializeField] protected AluVisualiser aluVisualizer;

    [FormerlySerializedAs("_registerFileVisualizer")] [SerializeField] protected RegisterFileVisualizer registerFileVisualizer;
    [FormerlySerializedAs("extenderVizualizer")] [FormerlySerializedAs("_extenderVizualizer")] [SerializeField] private ExtenderVisualizer extenderVisualizer;
    [FormerlySerializedAs("_MUXVisualizer")] [SerializeField] private MultiplexerVisualizer muxVisualizer;

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoImmRegister;
    private InfoPanelUI _infoA3Register;
    private InfoPanelUI _infoWd3Register;
    #endregion

    // Intern components for computations
    private Register _srcA;
    private Register _immValue;
    private Register _a3;
    private Register _wd3;

    private RegisterFile _registerFile;

    private int _currentBus;

    protected override void OnLevelStart()
    {
        // addi x0, x4, 256
        _srcA = new Register(4);
        _immValue = new Register(268566547);
        _a3 = new Register();
        _wd3 = new Register();

        _registerFile = new RegisterFile();
        _registerFile.InitializeRegisters(new [] { -98, 1, 39, 43, 0, 6, 8,
                                                     40, 3, 39, 13, 56, 64, 20,
                                                     50, 51, 0, 12, 53, 65, 29,
                                                     60, 61, 0, 25, 54, 0, 28,
                                                     70, 30, 31, 0});

        // Caching of UI panels for visualizers
        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoImmRegister = registerImmediateVisualizer.UIRegisterPanel;
        _infoA3Register = registerA3Visualizer.UIRegisterPanel;
        _infoWd3Register = registerWd3Visualizer.UIRegisterPanel;
        
        UpdateVisualizers();
    }

    protected override void ApplyState(SubExtendedSevenLevelState s)
    {
        _srcA.Reset(s.RegisterSrcAValue);
        _immValue.Reset(s.RegisterImmValue);
        _a3.Reset(s.RegisterA3Value);
        _wd3.Reset(s.RegisterWd3Value);

        _registerFile.InitializeRegisters(s.RegisterFieldValue);

        _srcA.WriteEnable = s.RegisterSrcAwe;
        _immValue.WriteEnable = s.RegisterImmWe;
        _a3.WriteEnable = s.RegisterA3We;
        _wd3.WriteEnable = s.RegisterWd3We;

        aluVisualizer.ChooseAluOperation(s.AluOperation);
        extenderVisualizer.ChooseAluOperation(s.ExtenderOperation);

        ApplyMuxState(s.MuxPath, muxVisualizer);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerImmediateVisualizer.TriggerBlink();
        registerA3Visualizer.TriggerBlink();
        registerWd3Visualizer.TriggerBlink();

        registerFileVisualizer.TriggerBlink();
    }

    protected override bool CheckWinCondition()
    {
        return _registerFile.Registers[0] == 256;
    }

    protected override SubExtendedSevenLevelState GetCurrentState()
    {
        return new SubExtendedSevenLevelState
        {
            RegisterSrcAValue = _srcA.Output,
            RegisterImmValue = _immValue.Output,
            RegisterA3Value = _a3.Output,
            RegisterWd3Value = _wd3.Output,

            RegisterFieldValue = (int[])_registerFile.Registers.Clone(),

            RegisterSrcAwe = _srcA.WriteEnable,
            RegisterImmWe = _immValue.WriteEnable,
            RegisterA3We = _a3.WriteEnable,
            RegisterWd3We = _wd3.WriteEnable,

            AluOperation = aluVisualizer.CurrentAluOperation,

            ExtenderOperation = extenderVisualizer.CurrentAluOperation,

            MuxPath = muxVisualizer.CurrentChosenMuxPath,
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _immValue.WriteEnable = registerImmediateVisualizer.isWriteEnabled;
        _a3.WriteEnable = registerA3Visualizer.isWriteEnabled;
        _wd3.WriteEnable = registerWd3Visualizer.isWriteEnabled;
        
        _registerFile.WriteEnable = registerFileVisualizer.isWriteEnabled;

        // implementation
        _registerFile.ReadAdress1 = _srcA.Output;
        _registerFile.ReadAdress2 = 0;

        _registerFile.ReadRegisters();

        var a = 0;
        if (_srcA.Output is > 0 and < 16)
            a = _registerFile.Registers[_srcA.Output];
        var ext = Extender.Evaluate(extenderVisualizer.CurrentAluOperation, (uint)_immValue.Output);
        var muxVal = EvaluateMux(muxVisualizer.CurrentChosenMuxPath, 0, ext, -1);

        _wd3.Input = Alu.Calculate(a, muxVal, aluVisualizer.CurrentAluOperation);

        if (TickCounter - 1 >= 0)
        {
            _registerFile.WriteAdress = StateAt(TickCounter - 1).RegisterA3Value;
        }

        _registerFile.WriteData = _wd3.Output;



        _srcA.PreClockUpdate();
        _immValue.PreClockUpdate();
        _a3.PreClockUpdate();
        _wd3.PreClockUpdate();


        // Only if WriteEnable = true, call Clock
        _srcA.Clock();
        _immValue.Clock();
        _a3.Clock();
        _wd3.Clock();
        _registerFile.Clock();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            var ext = Extender.Evaluate(extenderVisualizer.CurrentAluOperation, (uint)_immValue.Output);
            var mux = EvaluateMux(0, ext, -1, muxVisualizer.CurrentChosenMuxPath);

            yield return Bus.WaveBack((buses.wd3RegToRegFile, _wd3.Input));
            yield return Bus.WaveBack((buses.aluToWd3Reg, _wd3.Input));
            yield return Bus.WaveBack((buses.rd1ToAlu, _registerFile.Registers[_srcA.Output]), (buses.srcBMuxToAlu, mux));
            yield return Bus.WaveBack((buses.zeroToSrcBMux, 0), (buses.extToSrcBMux, ext));
            yield return Bus.Wave(
                (buses.srcAToRegFileA1, _srcA.Output), (buses.a3ToRegFileA3, _a3.Output), (buses.immToExtend, _immValue.Output));

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
            var ext = Extender.Evaluate(extenderVisualizer.CurrentAluOperation, (uint)_immValue.Output);
            var mux = EvaluateMux(0, ext, -1, muxVisualizer.CurrentChosenMuxPath);

            yield return Bus.Wave(
                (buses.srcAToRegFileA1, _srcA.Output), (buses.a3ToRegFileA3, _a3.Output), (buses.immToExtend, _immValue.Output));
            yield return Bus.Wave((buses.zeroToSrcBMux, 0), (buses.extToSrcBMux, ext));
            yield return Bus.Wave((buses.rd1ToAlu, a), (buses.srcBMuxToAlu, mux));
            yield return Bus.Wave((buses.aluToWd3Reg, Alu.Calculate(a, mux, aluVisualizer.CurrentAluOperation)));
            yield return Bus.Wave((buses.wd3RegToRegFile, _wd3.Output));

            _currentBus++;
        }
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register A1", _srcA.Output);
        _infoImmRegister.Display("Register A2", RiscVDecoder.CommandBuilder((uint)_immValue.Output));
        _infoA3Register.Display("Register A3", _a3.Output);
        _infoWd3Register.Display("Register WD3", _wd3.Output);

        registerFileVisualizer.UIRegisterPanel.Display(_registerFile.Registers);


        // ==============================  WE SECTION  =====================================
        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerImmediateVisualizer.ForceUpdateWriteEnableVisualization(_immValue.WriteEnable);
        registerA3Visualizer.ForceUpdateWriteEnableVisualization(_a3.WriteEnable);
        registerWd3Visualizer.ForceUpdateWriteEnableVisualization(_wd3.WriteEnable);

        registerFileVisualizer.ForceUpdateWriteEnableVisualization(_registerFile.WriteEnable);
    }
}
