using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct SubExtendedSevenLevelState
{
    public RegisterState SrcA;
    public RegisterState Immediate;
    public RegisterState A3;
    public RegisterState Wd3;
    public RegisterFileState RegisterFile;
    public AluOperation AluOperation;
    public ImmediateType ImmediateType;
    public int MuxInput;
}

[System.Serializable]
public class SubExtendedBusSegments
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

    // Intern components for computations
    private Register _srcA;
    private Register _immValue;
    private Register _a3;
    private Register _wd3;

    private RegisterFile _registerFile;


    private readonly Alu _alu = new();
    private readonly Extender _extender = new();

    private readonly Multiplexer _mux = new();

    protected override void OnLevelStart()
    {
        muxVisualizer.Bind(_mux);
        aluVisualizer.Bind(_alu);
        extenderVisualizer.Bind(_extender);

        // addi x0, x4, 256
        _srcA = new Register(4);
        _immValue = new Register(268566547);
        _a3 = new Register();
        _wd3 = new Register();

        _registerFile = new RegisterFile(new [] { -98, 1, 39, 43, 0, 6, 8,
                                                     40, 3, 39, 13, 56, 64, 20,
                                                     50, 51, 0, 12, 53, 65, 29,
                                                     60, 61, 0, 25, 54, 0, 28,
                                                     70, 30, 31, 0});

        Clock.Add(_srcA, _immValue, _a3, _wd3, _registerFile);

        Show(
            registerSrcAVisualizer.Bind(_srcA, "Register A1"),
            registerImmediateVisualizer.Bind(_immValue, "Register A2", word => RiscVDecoder.CommandBuilder((uint)word)),
            registerA3Visualizer.Bind(_a3, "Register A3"),
            registerWd3Visualizer.Bind(_wd3, "Register WD3"),
            registerFileVisualizer.Bind(_registerFile));
    }

    protected override void ApplyState(SubExtendedSevenLevelState s)
    {
        _srcA.Restore(s.SrcA);
        _immValue.Restore(s.Immediate);
        _a3.Restore(s.A3);
        _wd3.Restore(s.Wd3);
        _registerFile.Restore(s.RegisterFile);
        _alu.Operation = s.AluOperation;
        _extender.Type = s.ImmediateType;
        _mux.SelectedInput = s.MuxInput;
    }

    protected override bool CheckWinCondition()
    {
        return _registerFile.Registers[0] == 256;
    }

    protected override SubExtendedSevenLevelState GetCurrentState() => new()
    {
        SrcA = _srcA.Save(),
        Immediate = _immValue.Save(),
        A3 = _a3.Save(),
        Wd3 = _wd3.Save(),
        RegisterFile = _registerFile.Save(),
        AluOperation = _alu.Operation,
        ImmediateType = _extender.Type,
        MuxInput = _mux.SelectedInput
    };

    private int ImmExt => _extender.Extend(_immValue.Output);

    /// <summary> ALU input B: 0 (R-type path) or the extended immediate (I-type path). </summary>
    private int SrcB => _mux.Route(0, ImmExt, -1);

    protected override void HandleClockUpdate()
    {
        _registerFile.ReadAddress1 = _srcA.Output;
        _registerFile.ReadAddress2 = 0;
        _registerFile.ReadRegisters();

        _wd3.Input = _alu.Compute(DisplayedRegister(_srcA.Output), SrcB);

        // WD3 is one tick behind the ALU, so the destination address is taken from the previous tick as well
        if (TickCounter >= 1) _registerFile.WriteAddress = StateAt(TickCounter - 1).A3.Value;
        _registerFile.WriteData = _wd3.Output;

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        yield return Bus.WaveBack((buses.wd3RegToRegFile, _wd3.Input));
        yield return Bus.WaveBack((buses.aluToWd3Reg, _wd3.Input));
        yield return Bus.WaveBack((buses.rd1ToAlu, DisplayedRegister(_srcA.Output)), (buses.srcBMuxToAlu, SrcB));
        yield return Bus.WaveBack((buses.zeroToSrcBMux, 0), (buses.extToSrcBMux, ImmExt));
        yield return Bus.WaveBack(
            (buses.srcAToRegFileA1, _srcA.Output), (buses.a3ToRegFileA3, _a3.Output), (buses.immToExtend, _immValue.Output));
    }

    /// <summary> Register value as shown on the RD buses: only r1..r15 are displayed, everything else reads 0. </summary>
    private int DisplayedRegister(int address) =>
        address is > 0 and < 16 ? _registerFile.Registers[address] : 0;

    protected override IEnumerator RunBusVisualizations()
    {
        var a = DisplayedRegister(_srcA.Output);
        var srcB = SrcB;

        yield return Bus.Wave(
            (buses.srcAToRegFileA1, _srcA.Output), (buses.a3ToRegFileA3, _a3.Output), (buses.immToExtend, _immValue.Output));
        yield return Bus.Wave((buses.zeroToSrcBMux, 0), (buses.extToSrcBMux, ImmExt));
        yield return Bus.Wave((buses.rd1ToAlu, a), (buses.srcBMuxToAlu, srcB));
        yield return Bus.Wave((buses.aluToWd3Reg, _alu.Compute(a, srcB)));
        yield return Bus.Wave((buses.wd3RegToRegFile, _wd3.Output));
    }
}
