using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class FullProcessorBusSegments
{
    [Header("IF - Instruction Fetch")]
    [Tooltip("PC -> ADR MUX input (selects instruction memory address)")]
    public LineRenderer pcToAdrMux;
    [Tooltip("PC -> OldPC register (saves PC for BTA stage)")]
    public LineRenderer pcToOldPcReg;
    [Tooltip("PC -> PC+4 adder input A")]
    public LineRenderer pcToPcAdder;
    [Tooltip("Constant 4 -> PC+4 adder input B")]
    public LineRenderer constFourToPcAdder;
    [Tooltip("ADR MUX output -> Memory address input")]
    public LineRenderer adrMuxToMem;
    [Tooltip("Memory read data -> Instruction Register")]
    public LineRenderer memToInstrReg;
    [Tooltip("SrcA MUX output -> SrcA register  /  SrcA register -> SrcA MUX")]
    public LineRenderer srcAMuxToSrcAReg;
    [Tooltip("SrcB MUX output -> SrcB register  /  SrcB register -> SrcB MUX")]
    public LineRenderer srcBMuxToSrcBReg;
    [Tooltip("ALU result -> AluOut register")]
    public LineRenderer aluToAluOutReg;
    [Tooltip("Result MUX output -> PC register input")]
    public LineRenderer resultMuxToPC;

    [Header("ID - Instruction Decode")]
    [Tooltip("Instruction Register -> Register File A1 (rs1)")]
    public LineRenderer instrToRegFileA1;
    [Tooltip("Instruction Register -> Register File A2 (rs2)")]
    public LineRenderer instrToRegFileA2;
    [Tooltip("Instruction Register -> Extend unit")]
    public LineRenderer instrToExtend;
    [Tooltip("Register File RD1 -> SrcA MUX input")]
    public LineRenderer rd1ToSrcAMux;
    [Tooltip("Register File RD2 -> SrcB MUX input")]
    public LineRenderer rd2ToSrcBMux;

    [Header("EX - Execute")]
    [Tooltip("Extend unit output -> SrcB MUX input [1]")]
    public LineRenderer extToSrcBMux;
    [Tooltip("SrcA MUX output -> ALU input A")]
    public LineRenderer srcAMuxToAlu;
    [Tooltip("SrcB MUX output -> ALU input B")]
    public LineRenderer srcBMuxToAlu;
    [Tooltip("ALU combinatorial output -> AluOut register")]
    public LineRenderer aluCombToAluOutReg;

    [Header("MEM - Memory")]
    [Tooltip("AluOut register -> Result MUX and Data Memory address")]
    public LineRenderer aluOutToResultMux;
    [Tooltip("AluOut register -> Data Memory address (A)")]
    public LineRenderer aluOutToDataMem;

    [Header("WB - Write Back")]
    [Tooltip("Result MUX -> PC (branch / next-PC)  - shared with IF")]
    public LineRenderer resultMuxToPcWb;
    [Tooltip("Result MUX -> Register File WD3")]
    public LineRenderer resultMuxToRegFile;
    [Tooltip("Result MUX -> third fanout destination")]
    public LineRenderer resultMuxFanOut;
}

/// <summary> Multi-cycle (Mehrtakt) processor level. The datapath itself is <see cref="MultiCycleProcessor"/>. </summary>
public class FullProcessorRegisseur
    : ProcessorRegisseurBase<MultiCycleProcessorState, FullProcessorBusSegments, ProcessorInitialState>
{
    private const int PhaseCount = 4; // fetch, decode, execute, write back

    [FormerlySerializedAs("_registerPCVisualizer")]
    [Header("Processor Specific Components")]
    [SerializeField] private RegisterVisualizer registerPCVisualizer;
    [FormerlySerializedAs("_registerOldPCVisualizer")] [SerializeField] private RegisterVisualizer registerOldPCVisualizer;
    [FormerlySerializedAs("registerIntructionVisualizer")] [FormerlySerializedAs("_registerIntructionVisualizer")] [SerializeField] private RegisterVisualizer registerInstructionVisualizer;
    [FormerlySerializedAs("_registerDataVisualizer")] [SerializeField] private RegisterVisualizer registerDataVisualizer;
    [FormerlySerializedAs("_registerSrcAVisualizer")] [SerializeField] private RegisterVisualizer registerSrcAVisualizer;
    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField] private RegisterVisualizer registerSrcBVisualizer;
    [FormerlySerializedAs("_registerALUOutVisualizer")] [SerializeField] private RegisterVisualizer registerAluOutVisualizer;

    [FormerlySerializedAs("_adrMUXVisualizer")] [SerializeField] private MultiplexerVisualizer adrMuxVisualizer;
    [FormerlySerializedAs("_srcAMUXVisualizer")] [SerializeField] private MultiplexerVisualizer srcAmuxVisualizer;
    [FormerlySerializedAs("_srcBMUXVisualizer")] [SerializeField] private MultiplexerVisualizer srcBmuxVisualizer;
    [FormerlySerializedAs("_resultMUXVisualizer")] [SerializeField] private MultiplexerVisualizer resultMuxVisualizer;

    [FormerlySerializedAs("_aluVizualizer")] [SerializeField] private AluVisualiser aluVisualizer;
    [FormerlySerializedAs("extenderVizualizer")] [FormerlySerializedAs("_extenderVizualizer")] [SerializeField] private ExtenderVisualizer extenderVisualizer;

    [FormerlySerializedAs("_memoryVisualizer")] [SerializeField] private InstructionDataMemoryVisualizer memoryVisualizer;
    [FormerlySerializedAs("_registerFileVisualizer")] [SerializeField] private RegisterFileVisualizer registerFileVisualizer;

    [FormerlySerializedAs("_numberBlinker")] [SerializeField] private Blinker numberBlinker;

    [FormerlySerializedAs("_sidePanelInformer")]
    [Header("Extra Panel Fields")]
    [SerializeField] private SidePanelStateInformer sidePanelInformer;

    private MultiCycleProcessor _cpu;

    private int _currentBus; // number of animated ticks, selects the phase

    protected override void OnLevelStart()
    {
        base.OnLevelStart();

        _cpu = new MultiCycleProcessor(
            Clock,
            Initial.pcRegisterInitialValue,
            memoryWords: new[] { Initial.firstMemoWord, Initial.secondMemoWord, Initial.thirdMemoWord, Initial.fourthMemoWord },
            registers: ProcessorDefaults.RegisterValues);

        aluVisualizer.Bind(_cpu.Alu);
        extenderVisualizer.Bind(_cpu.Extender);

        adrMuxVisualizer.Bind(_cpu.AdrMux);
        srcAmuxVisualizer.Bind(_cpu.SrcAMux);
        srcBmuxVisualizer.Bind(_cpu.SrcBMux);
        resultMuxVisualizer.Bind(_cpu.ResultMux);

        Show(
            registerPCVisualizer.Bind(_cpu.Pc, "PC Register"),
            registerOldPCVisualizer.Bind(_cpu.OldPc, "Old PC Register"),
            registerInstructionVisualizer.Bind(_cpu.Instruction, "Instruction Register", Disassemble),
            registerDataVisualizer.Bind(_cpu.Data, "Data Register"),
            registerSrcAVisualizer.Bind(_cpu.SrcA, "SrcA Register"),
            registerSrcBVisualizer.Bind(_cpu.SrcB, "SrcB Register"),
            registerAluOutVisualizer.Bind(_cpu.AluOut, "ALU Out Register"),
            memoryVisualizer.Bind(_cpu.Memory, MemoryDisplay.Instructions),
            registerFileVisualizer.Bind(_cpu.RegisterFile));
    }

    private static string Disassemble(int word) => RiscVDecoder.CommandBuilder((uint)word);

    protected override void HandleClockUpdate() => _cpu.Step();

    protected override MultiCycleProcessorState GetCurrentState() => _cpu.Save();

    protected override void ApplyState(MultiCycleProcessorState state) => _cpu.Restore(state);

    protected override bool CheckWinCondition() =>
        ExerciseCheck.IsSolved(Initial, _cpu.RegisterFile.Registers, _cpu.Memory, _cpu.Pc.Output);

    protected override void BlinkClockedComponents()
    {
        base.BlinkClockedComponents();
        numberBlinker.Trigger();
    }

    /// <summary> Phase (0 fetch .. 3 write back) of the tick with the given index. </summary>
    private static int PhaseOf(int tick) => tick % PhaseCount;

    #region Bus animation
    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            switch (PhaseOf(_currentBus))
            {
                case 0: yield return RunFetch(); break;
                case 1: yield return RunDecode(); break;
                case 2: yield return RunExecute(); break;
                case 3: yield return RunWriteBack(); break;
            }

            _currentBus++;
        }
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            switch (PhaseOf(_currentBus - 1))
            {
                case 0: yield return ReverseFetch(); break;
                case 1: yield return ReverseDecode(); break;
                case 2: yield return ReverseExecute(); break;
                case 3: yield return ReverseWriteBack(); break;
            }

            _currentBus--;
        }
    }

    private IEnumerator RunFetch()
    {
        var pc = _cpu.Pc.Output;
        var sig = _cpu.ComputeSignals();

        yield return Bus.Wave((buses.pcToAdrMux, pc), (buses.pcToOldPcReg, pc), (buses.pcToPcAdder, pc));
        yield return Bus.Wave((buses.adrMuxToMem, sig.Address));
        yield return Bus.Wave((buses.memToInstrReg, _cpu.Memory.Read(sig.Address)));
        yield return Bus.Wave((buses.constFourToPcAdder, 4));
        yield return Bus.Wave((buses.srcAMuxToSrcAReg, sig.SrcA), (buses.srcBMuxToSrcBReg, sig.SrcB));
        yield return Bus.Wave((buses.aluToAluOutReg, sig.AluResult));
        yield return Bus.Wave((buses.resultMuxToPC, sig.Result));
    }

    private IEnumerator RunDecode()
    {
        var instr = _cpu.Instruction.Output;
        var registers = _cpu.RegisterFile.Registers;

        yield return Bus.Wave((buses.instrToRegFileA1, instr), (buses.instrToRegFileA2, instr), (buses.instrToExtend, instr));
        yield return Bus.Wave(
            (buses.rd1ToSrcAMux, registers[InstructionFields.Rs1(instr)]),
            (buses.rd2ToSrcBMux, registers[InstructionFields.Rs2(instr)]));
    }

    private IEnumerator RunExecute()
    {
        var sig = _cpu.ComputeSignals();

        yield return Bus.Wave((buses.srcAMuxToSrcAReg, _cpu.SrcA.Output), (buses.extToSrcBMux, _cpu.ImmExt));
        yield return Bus.Wave((buses.srcAMuxToAlu, sig.SrcA), (buses.srcBMuxToAlu, sig.SrcB));
        yield return Bus.Wave((buses.aluCombToAluOutReg, sig.AluResult));
    }

    private IEnumerator RunWriteBack()
    {
        var aluOut = _cpu.AluOut.Output;
        var result = _cpu.ComputeSignals().Result;

        yield return Bus.Wave((buses.aluOutToResultMux, aluOut), (buses.aluOutToDataMem, aluOut));
        yield return Bus.Wave((buses.resultMuxToPC, result), (buses.resultMuxToRegFile, result), (buses.resultMuxFanOut, result));
    }

    private IEnumerator ReverseFetch()
    {
        var pc = _cpu.Pc.Output;
        var sig = _cpu.ComputeSignals();

        yield return Bus.WaveBack((buses.resultMuxToPC, _cpu.Pc.Input));
        yield return Bus.WaveBack((buses.aluToAluOutReg, sig.AluResult));
        yield return Bus.WaveBack((buses.srcAMuxToSrcAReg, _cpu.SrcA.Output), (buses.srcBMuxToSrcBReg, _cpu.SrcB.Output));
        yield return Bus.WaveBack((buses.memToInstrReg, _cpu.Instruction.Input));
        yield return Bus.WaveBack((buses.constFourToPcAdder, 4));
        yield return Bus.WaveBack((buses.adrMuxToMem, _cpu.Memory.Address));
        yield return Bus.WaveBack((buses.pcToAdrMux, pc), (buses.pcToOldPcReg, pc), (buses.pcToPcAdder, pc));
    }

    private IEnumerator ReverseDecode()
    {
        var instr = _cpu.Instruction.Output;

        yield return Bus.WaveBack((buses.rd1ToSrcAMux, _cpu.SrcA.Input), (buses.rd2ToSrcBMux, _cpu.SrcB.Input));
        yield return Bus.WaveBack((buses.instrToRegFileA1, instr), (buses.instrToRegFileA2, instr), (buses.instrToExtend, instr));
    }

    private IEnumerator ReverseExecute()
    {
        var sig = _cpu.ComputeSignals();

        yield return Bus.WaveBack((buses.aluCombToAluOutReg, sig.AluResult));
        yield return Bus.WaveBack((buses.srcAMuxToSrcAReg, _cpu.SrcA.Output), (buses.extToSrcBMux, _cpu.ImmExt));
        yield return Bus.WaveBack((buses.srcAMuxToAlu, sig.SrcA), (buses.srcBMuxToAlu, sig.SrcB));
    }

    private IEnumerator ReverseWriteBack()
    {
        var aluOut = _cpu.AluOut.Output;
        var result = _cpu.ComputeSignals().Result;

        yield return Bus.WaveBack((buses.resultMuxToPC, result), (buses.resultMuxToRegFile, result), (buses.resultMuxFanOut, result));
        yield return Bus.WaveBack((buses.aluOutToResultMux, aluOut), (buses.aluOutToDataMem, aluOut));
    }
    #endregion

    #region Side panel
    protected override void OnViewsRefreshed()
    {
        sidePanelInformer.SetStateInfo(CurrentStateName());
    }

    /// <summary> Which FSM state of the multi-cycle control unit the next tick corresponds to. </summary>
    private StateName CurrentStateName()
    {
        var phase = PhaseOf(_currentBus);
        var pc = _cpu.Pc.Output;
        var instr = _cpu.Instruction.Output;

        // FETCH: is there a valid instruction at the PC?
        if (phase == 0)
        {
            var hasFetch = pc % 4 == 0
                           && pc is >= 0 and <= 12
                           && _cpu.Memory.Contains(pc)
                           && _cpu.Memory.Read(pc) > GameConstants.MinValidInstruction;
            return hasFetch ? StateName.FETCH : StateName.UNKNOWN;
        }

        // phases 1-3 need a valid instruction in the Instr register
        if (instr <= GameConstants.MinValidInstruction) return StateName.UNKNOWN;

        var opcode = instr & 0x7F;

        return phase switch
        {
            1 => StateName.DECODE,
            2 => opcode switch
            {
                0x33 => StateName.EXECUTE_R,    // R-type
                0x13 => StateName.EXECUTE_I,    // I-type
                0x03 => StateName.MEM_ADDRESS,  // LOAD
                0x23 => StateName.MEM_ADDRESS,  // STORE
                0x63 => StateName.BEQ,          // branch
                0x6F => StateName.JAL,          // JAL
                0x67 => StateName.JAL,          // JALR
                _ => StateName.UNKNOWN
            },
            _ => opcode switch
            {
                0x33 => StateName.ALU_WB,       // R-type write back
                0x13 => StateName.ALU_WB,       // I-type write back
                0x03 => StateName.MEM_READ,     // LOAD memory read
                0x23 => StateName.MEM_WRITE,    // STORE memory write
                0x63 => StateName.BEQ,          // branch PC update
                0x6F => StateName.ALU_WB,       // JAL write back (rd = PC+4)
                0x67 => StateName.ALU_WB,       // JALR write back
                _ => StateName.UNKNOWN
            }
        };
    }
    #endregion
}
