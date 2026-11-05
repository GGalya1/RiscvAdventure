using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public struct ProcessorLevelState
{
    public RegisterState Pc, OldPc, Instruction, Data, SrcA, SrcB, AluOut;
    public MemoryState Memory;
    public RegisterFileState RegisterFile;

    public AluOperation AluOperation;
    public ImmediateType ExtenderOperation;

    public int MuXadrPath;
    public int MuXsrcAPath;
    public int MuXsrcBPath;
    public int MuXresultPath;
}

internal readonly struct MehrtaktSignals
{
    public readonly int AdrMuxOut;
    public readonly int SrcAMuxOut;
    public readonly int SrcBMuxOut;
    public readonly int AluResult;
    public readonly int ResultMuxOut;

    public MehrtaktSignals(
        int adrMuxOut, int srcAMuxOut, int srcBMuxOut, 
        int aluResult, int resultMuxOut
    )
    {
        AdrMuxOut = adrMuxOut;
        SrcAMuxOut = srcAMuxOut;
        SrcBMuxOut = srcBMuxOut;
        AluResult = aluResult;
        ResultMuxOut = resultMuxOut;
    }
}

[System.Serializable]
public class FullProcessorBusSegments: IBusSegmentProvider
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

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(pcToAdrMux);
        c.RegisterSegment(pcToOldPcReg);
        c.RegisterSegment(pcToPcAdder);
        c.RegisterSegment(constFourToPcAdder);
        c.RegisterSegment(adrMuxToMem);
        c.RegisterSegment(memToInstrReg);
        c.RegisterSegment(srcAMuxToSrcAReg);
        c.RegisterSegment(srcBMuxToSrcBReg);
        c.RegisterSegment(aluToAluOutReg);
        c.RegisterSegment(resultMuxToPC);
        c.RegisterSegment(instrToRegFileA1);
        c.RegisterSegment(instrToRegFileA2);
        c.RegisterSegment(instrToExtend);
        c.RegisterSegment(rd1ToSrcAMux);
        c.RegisterSegment(rd2ToSrcBMux);
        c.RegisterSegment(extToSrcBMux);
        c.RegisterSegment(srcAMuxToAlu);
        c.RegisterSegment(srcBMuxToAlu);
        c.RegisterSegment(aluCombToAluOutReg);
        c.RegisterSegment(aluOutToResultMux);
        c.RegisterSegment(aluOutToDataMem);
        c.RegisterSegment(resultMuxToPcWb);
        c.RegisterSegment(resultMuxToRegFile);
        c.RegisterSegment(resultMuxFanOut);
    }
}

public class FullProcessorRegisseur
    : ProcessorRegisseurBase<ProcessorLevelState, FullProcessorBusSegments, ProcessorInitialState>
{
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

    // Intern components for computations
    private Register _pc;
    private Register _oldPC;
    private Register _instructionReg;
    private Register _dataReg;
    private Register _srcA;
    private Register _srcB;
    private Register _aluOutReg;

    private DataInstMemory _dataInstructionMemory;
    private RegisterFile _registerFile;

    private int _currentBus; // [0, 10]

    protected override void Start()
    {
        base.Start();
        buses.RegisterAll(busController);
    }
    private readonly Alu _alu = new();
    private readonly Extender _extender = new();

    private readonly Multiplexer _adrMux = new();
    private readonly Multiplexer _srcAMux = new();
    private readonly Multiplexer _srcBMux = new();
    private readonly Multiplexer _resultMux = new();

    protected override void OnLevelStart()
    {
        base.OnLevelStart();

        adrMuxVisualizer.Bind(_adrMux);
        srcAmuxVisualizer.Bind(_srcAMux);
        srcBmuxVisualizer.Bind(_srcBMux);
        resultMuxVisualizer.Bind(_resultMux);

        aluVisualizer.Bind(_alu);
        extenderVisualizer.Bind(_extender);

        _pc = new Register(Initial.pcRegisterInitialValue);
        _oldPC = new Register();
        _instructionReg = new Register();
        _dataReg = new Register();
        _srcA = new Register();
        _srcB = new Register();
        _aluOutReg = new Register();

        _registerFile = new RegisterFile(ProcessorDefaults.RegisterValues);

        _dataInstructionMemory = new DataInstMemory();

        _dataInstructionMemory.LoadWords(Initial.firstMemoWord, Initial.secondMemoWord, Initial.thirdMemoWord, Initial.fourthMemoWord);

        Clock.Add(_pc, _oldPC, _instructionReg, _dataReg, _srcA, _srcB, _aluOutReg, _dataInstructionMemory, _registerFile);

        Show(
            registerPCVisualizer.Bind(_pc, "PC Register"),
            registerOldPCVisualizer.Bind(_oldPC, "Old PC Register"),
            registerInstructionVisualizer.Bind(_instructionReg, "Instruction Register", word => RiscVDecoder.CommandBuilder((uint)word)),
            registerDataVisualizer.Bind(_dataReg, "Data Register"),
            registerSrcAVisualizer.Bind(_srcA, "SrcA Register"),
            registerSrcBVisualizer.Bind(_srcB, "SrcB Register"),
            registerAluOutVisualizer.Bind(_aluOutReg, "ALU Out Register"),
            memoryVisualizer.Bind(_dataInstructionMemory, MemoryDisplay.Instructions),
            registerFileVisualizer.Bind(_registerFile));
    }

    private MehrtaktSignals ComputeMehrtaktSignals()
    {
        var ext = _extender.Extend(_instructionReg.Output);
        var srcA = _srcAMux.Route(_pc.Output, _oldPC.Output, _srcA.Output);
        var srcB = _srcBMux.Route(_srcB.Output, ext, 4);
        var alu = _alu.Compute(srcA, srcB);
        var result = _resultMux.Route(_aluOutReg.Output, _dataReg.Output, alu);
        var adr = _adrMux.Route(_pc.Output, result, 0);

        return new MehrtaktSignals(
            adr, srcA,
            srcB, alu, result
        );
    }

    protected override void ApplyState(ProcessorLevelState s)
    {
        _pc.Restore(s.Pc);
        _oldPC.Restore(s.OldPc);
        _instructionReg.Restore(s.Instruction);
        _dataReg.Restore(s.Data);
        _srcA.Restore(s.SrcA);
        _srcB.Restore(s.SrcB);
        _aluOutReg.Restore(s.AluOut);
        _dataInstructionMemory.Restore(s.Memory);
        _registerFile.Restore(s.RegisterFile);

        _alu.Operation = s.AluOperation;
        _extender.Type = s.ExtenderOperation;

        _adrMux.SelectedInput = s.MuXadrPath;
        _srcAMux.SelectedInput = s.MuXsrcAPath;
        _srcBMux.SelectedInput = s.MuXsrcBPath;
        _resultMux.SelectedInput = s.MuXresultPath;
    }

    protected override void BlinkClockedComponents()
    {
        base.BlinkClockedComponents();
        numberBlinker.Trigger();
    }

    protected override bool CheckWinCondition() =>
        ExerciseCheck.IsSolved(Initial, _registerFile.Registers, _dataInstructionMemory, _pc.Output);

    protected override ProcessorLevelState GetCurrentState() => new()
    {
        Pc = _pc.Save(),
        OldPc = _oldPC.Save(),
        Instruction = _instructionReg.Save(),
        Data = _dataReg.Save(),
        SrcA = _srcA.Save(),
        SrcB = _srcB.Save(),
        AluOut = _aluOutReg.Save(),
        Memory = _dataInstructionMemory.Save(),
        RegisterFile = _registerFile.Save(),

        AluOperation = _alu.Operation,
        ExtenderOperation = _extender.Type,

        MuXadrPath = _adrMux.SelectedInput,
        MuXsrcAPath = _srcAMux.SelectedInput,
        MuXsrcBPath = _srcBMux.SelectedInput,
        MuXresultPath = _resultMux.SelectedInput
    };

    protected override void HandleClockUpdate()
    {
        var sig = ComputeMehrtaktSignals();

        // implementation

        #region first step (memory)
        var tmpAddress = sig.AdrMuxOut;
        _instructionReg.Input = _dataInstructionMemory.Read(tmpAddress);
        _dataReg.Input = _dataInstructionMemory.Read(tmpAddress);

        _oldPC.Input = _pc.Output;

        _dataInstructionMemory.Address = tmpAddress;
        #endregion

        #region second step (register file)

        // A1: [19:15] (Register Source 1)
        _registerFile.ReadAddress1 = InstructionFields.Rs1(_instructionReg.Output);

        // A2: [24:20] (Register Source 2)
        _registerFile.ReadAddress2 = InstructionFields.Rs2(_instructionReg.Output);

        _registerFile.ReadRegisters();

        // A3: [11:7] (Register Destination / rd)

        _srcA.Input = _registerFile.ReadData1;
        _srcB.Input = _registerFile.ReadData2;
        #endregion

        #region third step (ALU)

        _aluOutReg.Input = sig.AluResult;
        _dataInstructionMemory.WriteData = _srcB.Output;
        #endregion

        #region fourth step (WB)

        /*if (TickCounter - 2 >= 0) {
            _registerFile.WriteAddress = ((StateAt(TickCounter - 2).RegisterInstrValue >> 7) & 0x1F);
            // _registerFile.WriteData = StateAt(TickCounter - 2).RegisterInstrValue;
        }*/

        _registerFile.WriteAddress = InstructionFields.Rd(_instructionReg.Output);

        var tmpResult = sig.ResultMuxOut;
        _registerFile.WriteData = tmpResult;
        _pc.Input = tmpResult;
        #endregion

        Clock.Tick();
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            switch (_currentBus % 4)
            {
                case 0: yield return RunFetchVisualisation(); break;
                case 1: yield return RunDecodeVisualisation(); break;
                case 2: yield return RunExecutionVisualisation(); break;
                case 3: yield return RunWriteBackVisualisation(); break;
            }

            _currentBus++;
        }
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            switch ((_currentBus - 1) % 4)
            {
                case 0: yield return ReverseFetchVisualisation(); break;
                case 1: yield return ReverseDecodeVisualisation(); break;
                case 2: yield return ReverseExecutionVisualisation(); break;
                case 3: yield return ReverseWriteBackVisualisation(); break;
            }

            _currentBus--;
        }
    }

    #region vizualization helpers
    private int ImmExt => _extender.Extend(_instructionReg.Output);

    private IEnumerator RunFetchVisualisation()
    {
        var pc = _pc.Output;
        var sig = ComputeMehrtaktSignals();

        yield return Bus.Wave((buses.pcToAdrMux, pc), (buses.pcToOldPcReg, pc), (buses.pcToPcAdder, pc));
        yield return Bus.Wave((buses.adrMuxToMem, sig.AdrMuxOut));
        yield return Bus.Wave((buses.memToInstrReg, _dataInstructionMemory.Read(sig.AdrMuxOut)));
        yield return Bus.Wave((buses.constFourToPcAdder, 4));
        yield return Bus.Wave((buses.srcAMuxToSrcAReg, sig.SrcAMuxOut), (buses.srcBMuxToSrcBReg, sig.SrcBMuxOut));
        yield return Bus.Wave((buses.aluToAluOutReg, sig.AluResult));
        yield return Bus.Wave((buses.resultMuxToPC, sig.ResultMuxOut));
    }

    private IEnumerator RunDecodeVisualisation()
    {
        var instr = _instructionReg.Output;
        var rs1 = InstructionFields.Rs1(instr);
        var rs2 = InstructionFields.Rs2(instr);

        yield return Bus.Wave((buses.instrToRegFileA1, instr), (buses.instrToRegFileA2, instr), (buses.instrToExtend, instr));
        yield return Bus.Wave((buses.rd1ToSrcAMux, _registerFile.Registers[rs1]), (buses.rd2ToSrcBMux, _registerFile.Registers[rs2]));
    }

    private IEnumerator RunExecutionVisualisation()
    {
        var sig = ComputeMehrtaktSignals();

        yield return Bus.Wave((buses.srcAMuxToSrcAReg, _srcA.Output), (buses.extToSrcBMux, ImmExt));
        yield return Bus.Wave((buses.srcAMuxToAlu, sig.SrcAMuxOut), (buses.srcBMuxToAlu, sig.SrcBMuxOut));
        yield return Bus.Wave((buses.aluCombToAluOutReg, sig.AluResult));
    }

    private IEnumerator RunWriteBackVisualisation()
    {
        var aluOut = _aluOutReg.Output;
        var res = ComputeMehrtaktSignals().ResultMuxOut;

        yield return Bus.Wave((buses.aluOutToResultMux, aluOut), (buses.aluOutToDataMem, aluOut));
        yield return Bus.Wave((buses.resultMuxToPC, res), (buses.resultMuxToRegFile, res), (buses.resultMuxFanOut, res));
    }

    private IEnumerator ReverseFetchVisualisation()
    {
        var pc = _pc.Output;
        var sig = ComputeMehrtaktSignals();

        yield return Bus.WaveBack((buses.resultMuxToPC, _pc.Input));
        yield return Bus.WaveBack((buses.aluToAluOutReg, sig.AluResult));
        yield return Bus.WaveBack((buses.srcAMuxToSrcAReg, _srcA.Output), (buses.srcBMuxToSrcBReg, _srcB.Output));
        yield return Bus.WaveBack((buses.memToInstrReg, _instructionReg.Input));
        yield return Bus.WaveBack((buses.constFourToPcAdder, 4));
        yield return Bus.WaveBack((buses.adrMuxToMem, _dataInstructionMemory.Address));
        yield return Bus.WaveBack((buses.pcToAdrMux, pc), (buses.pcToOldPcReg, pc), (buses.pcToPcAdder, pc));
    }

    private IEnumerator ReverseDecodeVisualisation()
    {
        var instr = _instructionReg.Output;

        yield return Bus.WaveBack((buses.rd1ToSrcAMux, _srcA.Input), (buses.rd2ToSrcBMux, _srcB.Input));
        yield return Bus.WaveBack((buses.instrToRegFileA1, instr), (buses.instrToRegFileA2, instr), (buses.instrToExtend, instr));
    }

    private IEnumerator ReverseExecutionVisualisation()
    {
        var sig = ComputeMehrtaktSignals();

        yield return Bus.WaveBack((buses.aluCombToAluOutReg, sig.AluResult));
        yield return Bus.WaveBack((buses.srcAMuxToSrcAReg, _srcA.Output), (buses.extToSrcBMux, ImmExt));
        yield return Bus.WaveBack((buses.srcAMuxToAlu, sig.SrcAMuxOut), (buses.srcBMuxToAlu, sig.SrcBMuxOut));
    }

    private IEnumerator ReverseWriteBackVisualisation()
    {
        var aluOut = _aluOutReg.Output;
        var res = ComputeMehrtaktSignals().ResultMuxOut;

        yield return Bus.WaveBack((buses.resultMuxToPC, res), (buses.resultMuxToRegFile, res), (buses.resultMuxFanOut, res));
        yield return Bus.WaveBack((buses.aluOutToResultMux, aluOut), (buses.aluOutToDataMem, aluOut));
    }
    #endregion

    /*private void UpdateSidePanel() {
        var containsKey = _pc.Output % 4 == 0 
                          && _pc.Output is <= 12 and >= 0
                          && _dataInstructionMemory.Contains(_pc.Output)
                          && _dataInstructionMemory.Read(_pc.Output) > GameConstants.MinValidInstruction;
        if (containsKey)
        {
            sidePanelInformer.SetStateInfo(StateName.FETCH);
        }
        else if (_instructionReg.Output > GameConstants.MinValidInstruction)
        {
            sidePanelInformer.SetStateInfo(StateName.DECODE);
        }
        else if (TickCounter - 2 >= 0) {
            var s = StateAt(TickCounter - 1);

            if (s.RegisterInstrValue < GameConstants.MinValidInstruction) return;
            var opcode = s.RegisterInstrValue & 0x7F;

            switch(opcode)
            {
                case 0x33:
                    sidePanelInformer.SetStateInfo(StateName.EXECUTE_R);
                    break;
                case 0x13:
                    sidePanelInformer.SetStateInfo(StateName.EXECUTE_I);
                    break;
                case 0x03:
                case 0x23:
                    sidePanelInformer.SetStateInfo(StateName.MEM_ADDRESS);
                    break;
                case 0x63:
                    sidePanelInformer.SetStateInfo(StateName.BEQ);
                    break;
                case 0x6F:
                case 0x67:
                    sidePanelInformer.SetStateInfo(StateName.JAL);
                    break;
            }
        }
        else if (TickCounter - 3 >= 0)
        {
            var s = StateAt(TickCounter - 2);

            if (s.RegisterInstrValue < GameConstants.MinValidInstruction) return;
            var opcode = s.RegisterInstrValue & 0x7F;

            switch (opcode)
            {
                case 0x33:
                case 0x13:
                    sidePanelInformer.SetStateInfo(StateName.ALU_WB);
                    break;
                case 0x03:
                    sidePanelInformer.SetStateInfo(StateName.MEM_READ);
                    break;
                case 0x23:
                    sidePanelInformer.SetStateInfo(StateName.MEM_WRITE);
                    break;
                case 0x6F:
                case 0x67:
                    sidePanelInformer.SetStateInfo(StateName.ALU_WB);
                    break;
            }
        }
        else if (TickCounter - 4 >= 0)
        {
            if (StateAt(TickCounter - 3).RegisterInstrValue < GameConstants.MinValidInstruction) return;
            var opcode = StateAt(TickCounter - 3).RegisterInstrValue & 0x7F;

            if (opcode == 0x03)
            {
                sidePanelInformer.SetStateInfo(StateName.MEM_WB);
            }
        }
        else
        {
            sidePanelInformer.SetStateInfo(StateName.UNKNOWN);
        }
    }*/

    protected override void OnViewsRefreshed() => UpdateSidePanel();

    private void UpdateSidePanel()
    {
        var stage = _currentBus % 4;

        // FETCH (Phase 0): Liegt am PC eine gültige Instruktion?
        if (stage == 0)
        {
            var hasFetch = _pc.Output % 4 == 0
                           && _pc.Output is >= 0 and <= 12
                           && _dataInstructionMemory.Contains(_pc.Output)
                           && _dataInstructionMemory.Read(_pc.Output) > GameConstants.MinValidInstruction;
            sidePanelInformer.SetStateInfo(hasFetch ? StateName.FETCH : StateName.UNKNOWN);
            return;
        }

        // Phasen 1-3: InstrReg muss eine gültige Instruktion enthalten
        if (_instructionReg.Output <= GameConstants.MinValidInstruction)
        {
            sidePanelInformer.SetStateInfo(StateName.UNKNOWN);
            return;
        }

        var opcode = _instructionReg.Output & 0x7F;

        switch (stage)
        {
            // DECODE: unabhängig vom Instruktionstyp immer gleich
            case 1:
                sidePanelInformer.SetStateInfo(StateName.DECODE);
                break;

            case 2:
                sidePanelInformer.SetStateInfo(opcode switch
                {
                    0x33 => StateName.EXECUTE_R,    // R-Typ
                    0x13 => StateName.EXECUTE_I,    // I-Typ
                    0x03 => StateName.MEM_ADDRESS,  // LOAD
                    0x23 => StateName.MEM_ADDRESS,  // STORE
                    0x63 => StateName.BEQ,          // Branch
                    0x6F => StateName.JAL,          // JAL
                    0x67 => StateName.JAL,          // JALR
                    _    => StateName.UNKNOWN
                });
                break;
            case 3:
                sidePanelInformer.SetStateInfo(opcode switch
                {
                    0x33 => StateName.ALU_WB,    // R-Typ  Write-Back
                    0x13 => StateName.ALU_WB,    // I-Typ  Write-Back
                    0x03 => StateName.MEM_READ,  // LOAD   Memory-Read
                    0x23 => StateName.MEM_WRITE, // STORE  Memory-Write
                    0x63 => StateName.BEQ,       // Branch PC-Update
                    0x6F => StateName.ALU_WB,    // JAL    Write-Back (rd = PC+4)
                    0x67 => StateName.ALU_WB,    // JALR   Write-Back
                    _    => StateName.UNKNOWN
                });
                break;
        }
    }
}
