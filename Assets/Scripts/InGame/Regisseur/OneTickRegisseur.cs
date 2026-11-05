using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class OneTickBusSegments : IBusSegmentProvider
{
    [Header("IF - Instruction Fetch")] [Tooltip("PC -> Instruction Memory address input")]
    public LineRenderer pcToInstrMem;

    [Tooltip("PC -> PC+4 adder")] public LineRenderer pcToPCPlus4;

    [Tooltip("PC+4 -> ResultMUX")] public LineRenderer pcPlus4ToResultMux;

    [Tooltip("PC -> BTA adder")] public LineRenderer pcToBta;

    [Header("ID - Instruction Decode")] [Tooltip("Instr[19:15] -> Register File A1 (rs1)")]
    public LineRenderer instrToA1;

    [Tooltip("Instr[24:20] -> Register File A2 (rs2)")]
    public LineRenderer instrToA2;

    [Tooltip("Instr[11:7]  -> Register File A3 (rd)")]
    public LineRenderer instrToA3;

    [Tooltip("Instr[31:7]  -> Extend unit")]
    public LineRenderer instrToExtend;

    [Header("EX - Execute (inputs)")] [Tooltip("RD1 -> ALU SrcA")]
    public LineRenderer rd1ToAluSrcA;

    [Tooltip("RD2 -> SrcB Mux input [0]")] public LineRenderer rd2ToSrcBMux;

    [Tooltip("RD2 -> Data Memory WD")] public LineRenderer rd2ToDataMemWd;

    [Tooltip("ImmExt -> SrcB Mux input [1]")]
    public LineRenderer immExtToSrcBMux;

    [Tooltip("ImmExt -> BTA adder")] public LineRenderer immExtToBta;

    [Tooltip("SrcB Mux output -> ALU SrcB")]
    public LineRenderer srcBMuxToAlu;

    [Header("EX - Execute (outputs)")] [Tooltip("PCPlus4 -> PC Mux input [0]")]
    public LineRenderer pcPlus4ToPcMux;

    [Tooltip("BTA -> PC Mux input [1]")] public LineRenderer btaToPcMux;

    [Header("MEM - Memory Access")] [Tooltip("ALUResult -> Data Memory address")]
    public LineRenderer aluResToDataMemAddr;

    [Tooltip("ALUResult -> Result Mux input [0]")]
    public LineRenderer aluResToResultMux;

    [Tooltip("ReadData -> Result Mux input [1]")]
    public LineRenderer readDataToResultMux;

    [Header("WB - Write Back")] [Tooltip("Result Mux output -> Register File WD3")]
    public LineRenderer resultToRegFileWd3;

    [Tooltip("PC Mux output -> PC input")] public LineRenderer pcNextToPc;

    /// <summary>
    ///     Registers all segments with BusController so their world-space paths are pre-computed.
    /// </summary>
    public void RegisterAll(BusController ctrl)
    {
        ctrl.RegisterSegment(pcToInstrMem);
        ctrl.RegisterSegment(pcToPCPlus4);
        ctrl.RegisterSegment(pcPlus4ToResultMux);
        ctrl.RegisterSegment(pcToBta);
        ctrl.RegisterSegment(instrToA1);
        ctrl.RegisterSegment(instrToA2);
        ctrl.RegisterSegment(instrToA3);
        ctrl.RegisterSegment(instrToExtend);
        ctrl.RegisterSegment(rd1ToAluSrcA);
        ctrl.RegisterSegment(rd2ToSrcBMux);
        ctrl.RegisterSegment(rd2ToDataMemWd);
        ctrl.RegisterSegment(immExtToSrcBMux);
        ctrl.RegisterSegment(immExtToBta);
        ctrl.RegisterSegment(srcBMuxToAlu);
        ctrl.RegisterSegment(pcPlus4ToPcMux);
        ctrl.RegisterSegment(btaToPcMux);
        ctrl.RegisterSegment(aluResToDataMemAddr);
        ctrl.RegisterSegment(aluResToResultMux);
        ctrl.RegisterSegment(readDataToResultMux);
        ctrl.RegisterSegment(resultToRegFileWd3);
        ctrl.RegisterSegment(pcNextToPc);
    }
}

/// <summary> Single-cycle (Eintakt) processor level. The datapath itself is <see cref="SingleCycleProcessor"/>. </summary>
public class OneTickRegisseur
    : ProcessorRegisseurBase<SingleCycleProcessorState, OneTickBusSegments, OneTickProcessorInitialState>
{
    [Header("Processor Specific Components")] [SerializeField]
    private RegisterVisualizer registerPCVisualizer;

    [SerializeField] private MultiplexerVisualizer adrMuxVisualizer;
    [SerializeField] private MultiplexerVisualizer srcBmuxVisualizer;
    [SerializeField] private MultiplexerVisualizer resultMuxVisualizer;

    [SerializeField] private AluVisualiser aluVisualizer;
    [SerializeField] private AluVisualiser pcAluVisualizer;
    [SerializeField] private AluVisualiser btaAluVisualizer;
    [SerializeField] private ExtenderVisualizer extenderVisualizer;

    [SerializeField] private InstructionDataMemoryVisualizer instructionMemoryVisualizer;
    [SerializeField] private InstructionDataMemoryVisualizer dataMemoryVisualizer;
    [SerializeField] private RegisterFileVisualizer registerFileVisualizer;

    [SerializeField] private Blinker numberBlinker;

    private SingleCycleProcessor _cpu;

    protected override void OnLevelStart()
    {
        base.OnLevelStart();

        _cpu = new SingleCycleProcessor(
            Clock,
            Initial.pcRegisterInitialValue,
            instructions: new[]
            {
                Initial.firstInstructionWord, Initial.secondInstructionWord,
                Initial.thirdInstructionWord, Initial.fourthInstructionWord
            },
            data: new[]
            {
                Initial.firstDataWord, Initial.secondDataWord,
                Initial.thirdDataWord, Initial.fourthDataWord
            },
            registers: ProcessorDefaults.RegisterValues);

        aluVisualizer.Bind(_cpu.Alu);
        pcAluVisualizer.Bind(_cpu.PcAlu);
        btaAluVisualizer.Bind(_cpu.BtaAlu);
        extenderVisualizer.Bind(_cpu.Extender);

        adrMuxVisualizer.Bind(_cpu.PcMux);
        srcBmuxVisualizer.Bind(_cpu.SrcBMux);
        resultMuxVisualizer.Bind(_cpu.ResultMux);

        Show(
            registerPCVisualizer.Bind(_cpu.Pc, "PC Register"),
            instructionMemoryVisualizer.Bind(_cpu.InstructionMemory, MemoryDisplay.Instructions),
            dataMemoryVisualizer.Bind(_cpu.DataMemory, MemoryDisplay.Instructions),
            registerFileVisualizer.Bind(_cpu.RegisterFile));
    }

    protected override void HandleClockUpdate() => _cpu.Step();

    protected override SingleCycleProcessorState GetCurrentState() => _cpu.Save();

    protected override void ApplyState(SingleCycleProcessorState state) => _cpu.Restore(state);

    protected override bool CheckWinCondition() =>
        ExerciseCheck.IsSolved(Initial, _cpu.RegisterFile.Registers, _cpu.DataMemory, _cpu.Pc.Output);

    protected override void BlinkClockedComponents()
    {
        base.BlinkClockedComponents();
        numberBlinker.Trigger();
    }

    #region Bus animation
    protected override IEnumerator RunBusVisualizations()
    {
        yield return Bus.Play(BuildWaves(_cpu.ComputeSignals()));
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        // replay the tick that is undone, starting from its saved pre-tick state
        yield return Bus.PlayBackwards(BuildWaves(_cpu.ComputeSignals(StateAt(TickCounter))));
    }

    /// <summary> The order in which the values of one cycle travel through the datapath. </summary>
    private BusPulse[][] BuildWaves(SingleCycleSignals sig)
    {
        return new[]
        {
            // IF: PC fans out
            new BusPulse[] { (buses.pcToInstrMem, sig.Pc), (buses.pcToPCPlus4, sig.Pc), (buses.pcToBta, sig.Pc) },

            // ID: instruction fans out to register file and extend
            new BusPulse[]
            {
                (buses.instrToA1, sig.Rs1), (buses.instrToA2, sig.Rs2),
                (buses.instrToA3, sig.Rd), (buses.instrToExtend, sig.Instr)
            },

            // EX part 1: register file and extend outputs
            new BusPulse[]
            {
                (buses.rd1ToAluSrcA, sig.Rd1), (buses.rd2ToSrcBMux, sig.Rd2), (buses.rd2ToDataMemWd, sig.Rd2),
                (buses.immExtToSrcBMux, sig.ImmExt), (buses.immExtToBta, sig.ImmExt)
            },

            // EX part 2: mux/adder outputs
            new BusPulse[]
            {
                (buses.srcBMuxToAlu, sig.SrcB), (buses.pcPlus4ToPcMux, sig.PcPlus4),
                (buses.btaToPcMux, sig.Bta), (buses.pcPlus4ToResultMux, sig.PcPlus4)
            },

            // MEM: ALU result drives data memory
            new BusPulse[] { (buses.aluResToDataMemAddr, sig.AluResult), (buses.aluResToResultMux, sig.AluResult) },
            new BusPulse[] { (buses.readDataToResultMux, sig.ReadData) },

            // WB: results flow back to register file and PC
            new BusPulse[] { (buses.resultToRegFileWd3, sig.Result), (buses.pcNextToPc, sig.PcNext) },
        };
    }
    #endregion
}
