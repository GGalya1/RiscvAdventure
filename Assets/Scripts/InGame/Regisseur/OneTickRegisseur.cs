using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OneTickBusSegments: IBusSegmentProvider
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

/*
    Immutable snapshot of every bus value for one execution cycle.
    Computed once per tick so HandleClockUpdate / RunBusVisualizations / ReverseBusVisualizations all use the same numbers without re-computing.
*/
internal readonly struct CycleSignals
{
    public readonly int Instr;
    public readonly int Rs1, Rs2, Rd;
    public readonly int Rd1, Rd2;
    public readonly int ImmExt;
    public readonly int SrcB;
    public readonly int AluResult;
    public readonly bool AluZero;
    public readonly int PcPlus4;
    public readonly int Bta;
    public readonly int ReadData;
    public readonly int Result;
    public readonly int PcNext;

    public CycleSignals(
        int instr, int rs1, int rs2, int rd,
        int rd1, int rd2, int immExt,
        int srcB, int aluResult, bool aluZero,
        int pcPlus4, int bta,
        int readData, int result, int pcNext)
    {
        Instr = instr;
        Rs1 = rs1;
        Rs2 = rs2;
        Rd = rd;
        Rd1 = rd1;
        Rd2 = rd2;
        ImmExt = immExt;
        SrcB = srcB;
        AluResult = aluResult;
        AluZero = aluZero;
        PcPlus4 = pcPlus4;
        Bta = bta;
        ReadData = readData;
        Result = result;
        PcNext = pcNext;
    }
}

public struct OneTickProcessorLevelState
{
    public int RegisterPCValue;

    public int FirstInstructionMemory;
    public int SecondInstructionMemory;
    public int ThirdInstructionMemory;
    public int FourthInstructionMemory;

    public int FirstDataMemory;
    public int SecondDataMemory;
    public int ThirdDataMemory;
    public int FourthDataMemory;

    public int[] RegisterFieldValue;

    public bool RegisterPCwe;
    
    public bool DataMemoryWrite;
    public bool RegisterFileWriteEnable;

    public int AluOperation;
    public int PCAluOperation;
    public int BtaOperation;

    public int ExtenderOperation;

    public int MuXadrPath;
    public int MuXsrcBPath;
    public int MuXresultPath;
}

public class OneTickRegisseur : BaseLevelRegisseur<OneTickProcessorLevelState, OneTickBusSegments>
{
    [Header("Initial values for level")]
    public static OneTickProcessorInitialState Initial;

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


    private int _currentBus; // [0, 3]
    private DataInstMemory _dataMemory;

    private DataInstMemory _instructionMemory;

    // Intern components for computations
    private Register _pc;
    private RegisterFile _registerFile;

    protected void Awake()
    {
        levelManager.SetLevelDialogue(Initial.customDialogueGraph);
    }
    
    protected override void Start()
    {
        base.Start();
        buses.RegisterAll(busController);
    }

    protected override void OnLevelStart()
    {
        if (Initial != null) levelTargetDescription = Initial.levelTarget;
        
        _pc = new Register(Initial.pcRegisterInitialValue) { WriteEnable = true };

        _instructionMemory = new DataInstMemory { MemoryWrite = false };
        _instructionMemory.LoadWord(0, Initial.firstInstructionWord);
        _instructionMemory.LoadWord(4, Initial.secondInstructionWord);
        _instructionMemory.LoadWord(8, Initial.thirdInstructionWord);
        _instructionMemory.LoadWord(12, Initial.fourthInstructionWord);

        _dataMemory = new DataInstMemory { MemoryWrite = false };
        _dataMemory.LoadWord(0, Initial.firstDataWord);
        _dataMemory.LoadWord(4, Initial.secondDataWord);
        _dataMemory.LoadWord(8, Initial.thirdDataWord);
        _dataMemory.LoadWord(12, Initial.fourthDataWord);

        _registerFile = new RegisterFile { RegisterWriteEnable = false };
        _registerFile.InitializeRegisters(new[]
        {
            0, 1, 39, 43, 5, 6, 8, 40,
            3, 39, 13, 56, 63, 20, 50, 51,
            0, 12, 53, 65, 29, 60, 61, 0,
            25, 54, 0, 28, 70, 30, 31, 0
        });
        
        // Caching of UI panels for visualizers
        _infoPCRegister = registerPCVisualizer.UIRegisterPanel;
        _infoInstructionMemory = instructionMemoryVisualizer.UIRegisterPanel;
        _infoDataMemory = dataMemoryVisualizer.UIRegisterPanel;
        

        UpdateVisualizers();
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _pc.WriteEnable = registerPCVisualizer.isWriteEnabled;
        // instruction memory is not tickted
        // _instructionMemory.MemoryWrite = instructionMemoryVisualizer.isWriteEnabled;
        _dataMemory.MemoryWrite = dataMemoryVisualizer.isWriteEnabled;
        _registerFile.RegisterWriteEnable = registerFileVisualizer.isWriteEnabled;


        // implementation

        // Compute all combinatorial values for this cycle (no side effects)
        var sig = ComputeSignals(
            _pc.Output, _registerFile.Registers,
            aluVisualizer.CurrentAluOperation,
            pcAluVisualizer.CurrentAluOperation,
            btaAluVisualizer.CurrentAluOperation,
            extenderVisualizer.CurrentAluOperation,
            srcBmuxVisualizer.CurrentChosenMuxPath,
            resultMuxVisualizer.CurrentChosenMuxPath,
            adrMuxVisualizer.CurrentChosenMuxPath,
            _dataMemory.Memory);

        // Prepare sequential element inputs
        _registerFile.ReadAdress1 = sig.Rs1;
        _registerFile.ReadAdress2 = sig.Rs2;
        _registerFile.WriteAdress = sig.Rd;
        _registerFile.WriteData = sig.Result;

        _dataMemory.Address = sig.AluResult;
        _dataMemory.WriteData = sig.Rd2;
        _pc.Input = sig.PcNext;


        // apply data
        _pc.PreClockUpdate();
        _dataMemory.PreClockUpdate();
        _registerFile.PreClockUpdate();

        _pc.Clock();
        _dataMemory.Clock();
        _registerFile.Clock();
    }

    protected override OneTickProcessorLevelState GetCurrentState()
    {
        return new OneTickProcessorLevelState
        {
            RegisterPCValue = _pc.Output,

            RegisterFieldValue = (int[])_registerFile.Registers.Clone(),

            FirstInstructionMemory = _instructionMemory.Memory[0],
            SecondInstructionMemory = _instructionMemory.Memory[4],
            ThirdInstructionMemory = _instructionMemory.Memory[8],
            FourthInstructionMemory = _instructionMemory.Memory[12],

            FirstDataMemory = _dataMemory.Memory[0],
            SecondDataMemory = _dataMemory.Memory[4],
            ThirdDataMemory = _dataMemory.Memory[8],
            FourthDataMemory = _dataMemory.Memory[12],

            RegisterPCwe = _pc.WriteEnable,
            DataMemoryWrite           = _dataMemory.MemoryWrite,
            RegisterFileWriteEnable   = _registerFile.RegisterWriteEnable,

            PCAluOperation = pcAluVisualizer.CurrentAluOperation,
            BtaOperation   = btaAluVisualizer.CurrentAluOperation,

            AluOperation = aluVisualizer.CurrentAluOperation,

            ExtenderOperation = extenderVisualizer.CurrentAluOperation,

            MuXadrPath = adrMuxVisualizer.CurrentChosenMuxPath,
            MuXsrcBPath = srcBmuxVisualizer.CurrentChosenMuxPath,
            MuXresultPath = resultMuxVisualizer.CurrentChosenMuxPath
        };
    }

    protected override void ApplyState(OneTickProcessorLevelState s)
    {
        _pc.Reset(s.RegisterPCValue);

        ApplyMuxState(s.MuXadrPath, adrMuxVisualizer);
        ApplyMuxState(s.MuXsrcBPath, srcBmuxVisualizer);
        ApplyMuxState(s.MuXresultPath, resultMuxVisualizer);

        _instructionMemory = new DataInstMemory
        {
            Memory =
            {
                [0] = s.FirstInstructionMemory,
                [4] = s.SecondInstructionMemory,
                [8] = s.ThirdInstructionMemory,
                [12] = s.FourthInstructionMemory
            }
        };
        _dataMemory = new DataInstMemory
        {
            MemoryWrite = s.DataMemoryWrite,
            Memory =
            {
                [0] = s.FirstDataMemory,
                [4] = s.SecondDataMemory,
                [8] = s.ThirdDataMemory,
                [12] = s.FourthDataMemory
            }
        };

        // noch Register File einfuegen
        _registerFile.InitializeRegisters(s.RegisterFieldValue);

        _pc.WriteEnable = s.RegisterPCwe;
        _registerFile.RegisterWriteEnable  = s.RegisterFileWriteEnable;


        aluVisualizer.ChooseAluOperation(s.AluOperation);
        pcAluVisualizer.ChooseAluOperation(s.PCAluOperation);
        btaAluVisualizer.ChooseAluOperation(s.BtaOperation);
        extenderVisualizer.ChooseAluOperation(s.ExtenderOperation);
    }

    protected override void BlinkClockedComponents()
    {
        registerPCVisualizer.TriggerBlink();

        // no tick for this component
        // instructionMemoryVisualizer.TriggerBlink();
        dataMemoryVisualizer.TriggerBlink();

        registerFileVisualizer.TriggerBlink();

        numberBlinker.Trigger();
    }

    protected override IEnumerator RunBusVisualizations()
    {
        var sig = ComputeSignals(
            _pc.Output, _registerFile.Registers,
            aluVisualizer.CurrentAluOperation,
            pcAluVisualizer.CurrentAluOperation,
            btaAluVisualizer.CurrentAluOperation,
            extenderVisualizer.CurrentAluOperation,
            srcBmuxVisualizer.CurrentChosenMuxPath,
            resultMuxVisualizer.CurrentChosenMuxPath,
            adrMuxVisualizer.CurrentChosenMuxPath,
            _dataMemory.Memory);

        // IF: PC fans out
        busController.StartBusSignal(buses.pcToInstrMem, _pc.Output);
        busController.StartBusSignal(buses.pcToPCPlus4, _pc.Output);
        busController.StartBusSignal(buses.pcToBta, _pc.Output);
        yield return WaitNoSignals;

        // ID: instruction fans out to register file and extend
        busController.StartBusSignal(buses.instrToA1, sig.Rs1);
        busController.StartBusSignal(buses.instrToA2, sig.Rs2);
        busController.StartBusSignal(buses.instrToA3, sig.Rd);
        busController.StartBusSignal(buses.instrToExtend, sig.Instr);
        yield return WaitNoSignals;

        // EX part 1: register file and extend outputs
        busController.StartBusSignal(buses.rd1ToAluSrcA, sig.Rd1);
        busController.StartBusSignal(buses.rd2ToSrcBMux, sig.Rd2);
        busController.StartBusSignal(buses.rd2ToDataMemWd, sig.Rd2);
        busController.StartBusSignal(buses.immExtToSrcBMux, sig.ImmExt);
        busController.StartBusSignal(buses.immExtToBta, sig.ImmExt);
        yield return WaitNoSignals;

        // EX part 2: mux/adder outputs
        busController.StartBusSignal(buses.srcBMuxToAlu, sig.SrcB);
        busController.StartBusSignal(buses.pcPlus4ToPcMux, sig.PcPlus4);
        busController.StartBusSignal(buses.btaToPcMux, sig.Bta);
        busController.StartBusSignal(buses.pcPlus4ToResultMux, sig.PcPlus4);
        yield return WaitNoSignals;

        // MEM: ALU result drives data memory
        busController.StartBusSignal(buses.aluResToDataMemAddr, sig.AluResult);
        busController.StartBusSignal(buses.aluResToResultMux, sig.AluResult);
        yield return WaitNoSignals;

        busController.StartBusSignal(buses.readDataToResultMux, sig.ReadData);
        yield return WaitNoSignals;

        // WB: results flow back to register file and PC
        busController.StartBusSignal(buses.resultToRegFileWd3, sig.Result);
        busController.StartBusSignal(buses.pcNextToPc, sig.PcNext);
        yield return WaitNoSignals;
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        // Reconstruct the bus values that were flowing during the tick we're undoing
        var s = TickStateValues[TickCounter]; // pre-tick state (TickCounter already --)

        // Rebuild data memory dictionary from saved state to get the correct ReadData
        var savedDataMem = new Dictionary<int, int>
        {
            [0] = s.FirstDataMemory,
            [4] = s.SecondDataMemory,
            [8] = s.ThirdDataMemory,
            [12] = s.FourthDataMemory
        };

        var sig = ComputeSignals(
            s.RegisterPCValue, s.RegisterFieldValue,
            s.AluOperation, s.PCAluOperation,
            s.BtaOperation, s.ExtenderOperation,
            s.MuXsrcBPath, s.MuXresultPath,
            s.MuXadrPath, savedDataMem);

        // WB reversed
        busController.StartBusSignal(buses.pcNextToPc, sig.PcNext, true);
        busController.StartBusSignal(buses.resultToRegFileWd3, sig.Result, true);
        yield return WaitNoSignals;

        // MEM reversed
        busController.StartBusSignal(buses.readDataToResultMux, sig.ReadData, true);
        yield return WaitNoSignals;

        busController.StartBusSignal(buses.aluResToDataMemAddr, sig.AluResult, true);
        busController.StartBusSignal(buses.aluResToResultMux, sig.AluResult, true);
        yield return WaitNoSignals;

        // EX part 2 reversed
        busController.StartBusSignal(buses.srcBMuxToAlu, sig.SrcB, true);
        busController.StartBusSignal(buses.pcPlus4ToPcMux, sig.PcPlus4, true);
        busController.StartBusSignal(buses.btaToPcMux, sig.Bta, true);
        busController.StartBusSignal(buses.pcPlus4ToResultMux, sig.PcPlus4,  true);
        yield return WaitNoSignals;

        // EX part 1 reversed
        busController.StartBusSignal(buses.rd1ToAluSrcA, sig.Rd1, true);
        busController.StartBusSignal(buses.rd2ToSrcBMux, sig.Rd2, true);
        busController.StartBusSignal(buses.rd2ToDataMemWd, sig.Rd2, true);
        busController.StartBusSignal(buses.immExtToSrcBMux, sig.ImmExt, true);
        busController.StartBusSignal(buses.immExtToBta, sig.ImmExt, true);
        yield return WaitNoSignals;

        // ID reversed
        busController.StartBusSignal(buses.instrToA1, sig.Rs1, true);
        busController.StartBusSignal(buses.instrToA2, sig.Rs2, true);
        busController.StartBusSignal(buses.instrToA3, sig.Rd, true);
        busController.StartBusSignal(buses.instrToExtend, sig.Instr, true);
        yield return WaitNoSignals;

        // IF reversed
        busController.StartBusSignal(buses.pcToInstrMem, s.RegisterPCValue, true);
        busController.StartBusSignal(buses.pcToPCPlus4, s.RegisterPCValue, true);
        busController.StartBusSignal(buses.pcToBta, s.RegisterPCValue, true);
        yield return WaitNoSignals;
    }

    protected override bool CheckWinCondition()
    {
        return Initial.aufgabeTyp switch
        {
            ExerciseTyp.REGISTER_FIELD => _registerFile.Registers[Initial.registerFieldAddressAnswer] ==
                                          Initial.registerFieldValueAnswer,
            ExerciseTyp.MEMORY => _dataMemory.Memory.GetValueOrDefault(Initial.memoryAddressAnswer, 0) ==
                                  Initial.memoryValueAnswer,
            ExerciseTyp.BEQ => _pc.Output == Initial.pcValueAnswer,
            ExerciseTyp.JAL => _pc.Output == Initial.pcValueAnswer &&
                               _registerFile.Registers[Initial.registerFieldAddressAnswer] ==
                               Initial.registerFieldValueAnswer,
            _ => false
        };
    }

    protected override void UpdateVisualizers()
    {
        _infoPCRegister.Display("PC Register", _pc.Output);
        registerPCVisualizer.ForceUpdateWriteEnableVisualization(_pc.WriteEnable);

        instructionMemoryVisualizer.UIRegisterPanel.Display(
            RiscVDecoder.CommandBuilder((uint)_instructionMemory.Memory[0]),
            RiscVDecoder.CommandBuilder((uint)_instructionMemory.Memory[4]),
            RiscVDecoder.CommandBuilder((uint)_instructionMemory.Memory[8]),
            RiscVDecoder.CommandBuilder((uint)_instructionMemory.Memory[12])
        );
        dataMemoryVisualizer.UIRegisterPanel.Display(
            RiscVDecoder.CommandBuilder((uint)_dataMemory.Memory[0]),
            RiscVDecoder.CommandBuilder((uint)_dataMemory.Memory[4]),
            RiscVDecoder.CommandBuilder((uint)_dataMemory.Memory[8]),
            RiscVDecoder.CommandBuilder((uint)_dataMemory.Memory[12])
        );
        registerFileVisualizer.UIRegisterPanel.Display(_registerFile.Registers);
        registerFileVisualizer.ForceUpdateWriteEnableVisualization(_registerFile.RegisterWriteEnable);
        instructionMemoryVisualizer.ForceUpdateWriteEnableVisualization(_instructionMemory.MemoryWrite);
        dataMemoryVisualizer.ForceUpdateWriteEnableVisualization(_dataMemory.MemoryWrite);
    }

    private CycleSignals ComputeSignals(
        int pc, int[] regValues,
        int aluOp, int pcAluOp, int btaOp, int extOp,
        int srcBPath, int resultPath, int adrPath,
        Dictionary<int, int> dataMem)
    {
        // IF: read instruction
        var instr = _instructionMemory.Memory.GetValueOrDefault(pc, 0);

        // ID: decode register addresses
        var rs1 = (instr >> 15) & 0x1F;
        var rs2 = (instr >> 20) & 0x1F;
        var rd = (instr >> 7) & 0x1F;

        // ID: read register file  (x0 is hardwired to 0)
        var rd1 = rs1 > 0 && rs1 < regValues.Length ? regValues[rs1] : 0;
        var rd2 = rs2 > 0 && rs2 < regValues.Length ? regValues[rs2] : 0;

        // ID: sign-extend immediate
        var immExt = Extender.Evaluate(extOp, (uint)instr);

        // EX: SrcB mux, main ALU, PC adders
        var srcB = EvaluateMux(srcBPath, rd2, immExt, 0);

        var (aluResult, aluZero) = Alu.Calculate(rd1, srcB, (AluOperation)aluOp);
        var pcPlus4 = Alu.Calculate(pc, 4, pcAluOp);
        var bta = Alu.Calculate(pc, immExt, btaOp);

        // MEM: data memory read
        var readData = dataMem.GetValueOrDefault(aluResult, 0);

        // WB: result and next PC selection
        var result = EvaluateMux(resultPath, aluResult, readData, pcPlus4);
        var pcNext = EvaluateMux(adrPath, pcPlus4, bta, 0);

        return new CycleSignals(
            instr, rs1, rs2, rd,
            rd1, rd2, immExt,
            srcB, aluResult, aluZero,
            pcPlus4, bta,
            readData, result, pcNext);
    }

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoPCRegister;

    private InstrMemoryControlPanel _infoInstructionMemory;
    private InstrMemoryControlPanel _infoDataMemory;

    #endregion

    #region transition to next level

    private void OnEnable()
    {
        LevelManager.OnRequestNextLevelData += GetNextLevelData;
    }

    private void OnDisable()
    {
        LevelManager.OnRequestNextLevelData -= GetNextLevelData;
    }

    private static object GetNextLevelData()
    {
        if (Initial != null && Initial.nextSceneInitial != null) return Initial.nextSceneInitial;
        return null;
    }

    #endregion
}