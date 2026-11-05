using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public struct ProcessorLevelState
{
    public int RegisterPCValue;
    public int RegisterOldPCValue;
    public int RegisterInstrValue;
    public int RegisterDataValue;
    public int RegisterScrAValue;
    public int RegisterSrcBValue;
    public int RegisterAluOutValue;

    public int FirstMemoryValue;
    public int SecondMemoryValue;
    public int ThirdMemoryValue;
    public int FourthMemoryValue;

    public int[] RegisterFieldValue;

    public bool RegisterPCwe;
    public bool RegisterOldPCwe;
    public bool RegisterInstrWe;
    public bool RegisterDataWe;
    public bool RegisterScrAwe;
    public bool RegisterSrcBwe;
    public bool RegisterAluOutWe;

    public int AluOperation;

    public int ExtenderOperation;

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

    #region CACHED UI REFERENCES
    private InfoPanelUI _infoPCRegister;
    private InfoPanelUI _infoOldPCRegister;
    private InfoPanelUI _infoInstructionRegister;
    private InfoPanelUI _infoDataRegister;
    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;
    private InfoPanelUI _infoAluOutRegister;

    private InstrMemoryControlPanel _infoDataMemory;
    #endregion

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
    protected override void OnLevelStart()
    {
        base.OnLevelStart();
        
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

        // Caching of UI panels for visualizers
        _infoPCRegister = registerPCVisualizer.UIRegisterPanel;
        _infoOldPCRegister = registerOldPCVisualizer.UIRegisterPanel;
        _infoInstructionRegister = registerInstructionVisualizer.UIRegisterPanel;
        _infoDataRegister = registerDataVisualizer.UIRegisterPanel;
        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;
        _infoAluOutRegister = registerAluOutVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }
    
    private MehrtaktSignals ComputeMehrtaktSignals()
    {
        var ext = Extender.Evaluate((ImmediateType)extenderVisualizer.CurrentAluOperation, (uint)_instructionReg.Output);
        var srcA = Multiplexer.Select(srcAmuxVisualizer.CurrentChosenMuxPath, _pc.Output, _oldPC.Output, _srcA.Output);
        var srcB = Multiplexer.Select(srcBmuxVisualizer.CurrentChosenMuxPath, _srcB.Output, ext, 4);
        var alu = Alu.Calculate(srcA, srcB, (AluOperation)aluVisualizer.CurrentAluOperation);
        var result = Multiplexer.Select(resultMuxVisualizer.CurrentChosenMuxPath, _aluOutReg.Output, _dataReg.Output, alu);
        var adr = Multiplexer.Select(adrMuxVisualizer.CurrentChosenMuxPath, _pc.Output, result, 0);

        return new MehrtaktSignals(
            adr, srcA,
            srcB, alu, result
        );
    }

    protected override void ApplyState(ProcessorLevelState s)
    {
        _pc.Reset(s.RegisterPCValue);
        _oldPC.Reset(s.RegisterOldPCValue);
        _instructionReg.Reset(s.RegisterInstrValue);
        _dataReg.Reset(s.RegisterDataValue);
        _srcA.Reset(s.RegisterScrAValue);
        _srcB.Reset(s.RegisterSrcBValue);
        _aluOutReg.Reset(s.RegisterAluOutValue);
        
        ApplyMuxState(s.MuXadrPath, adrMuxVisualizer);
        ApplyMuxState(s.MuXsrcAPath, srcAmuxVisualizer);
        ApplyMuxState(s.MuXsrcBPath, srcBmuxVisualizer);
        ApplyMuxState(s.MuXresultPath, resultMuxVisualizer);
        
        // WE for dataInstrMem ???
        _dataInstructionMemory.LoadWords(s.FirstMemoryValue, s.SecondMemoryValue, s.ThirdMemoryValue, s.FourthMemoryValue);

        // noch Register File einfuegen
        _registerFile.Load(s.RegisterFieldValue);

        _pc.WriteEnable = s.RegisterPCwe;
        _oldPC.WriteEnable = s.RegisterOldPCwe;
        _instructionReg.WriteEnable = s.RegisterInstrWe;
        _dataReg.WriteEnable = s.RegisterDataWe;
        _srcA.WriteEnable = s.RegisterScrAwe;
        _srcB.WriteEnable = s.RegisterSrcBwe;
        _aluOutReg.WriteEnable = s.RegisterAluOutWe;

        aluVisualizer.ChooseAluOperation(s.AluOperation);
        extenderVisualizer.ChooseAluOperation(s.ExtenderOperation);
    }

    protected override void BlinkClockedComponents()
    {
        registerPCVisualizer.TriggerBlink();
        registerOldPCVisualizer.TriggerBlink();
        registerInstructionVisualizer.TriggerBlink();
        registerDataVisualizer.TriggerBlink();
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerAluOutVisualizer.TriggerBlink();

        memoryVisualizer.TriggerBlink();
        registerFileVisualizer.TriggerBlink();

        numberBlinker.Trigger();
    }

    protected override bool CheckWinCondition() =>
        ExerciseCheck.IsSolved(Initial, _registerFile.Registers, _dataInstructionMemory, _pc.Output);

    protected override ProcessorLevelState GetCurrentState()
    {
        return new ProcessorLevelState
        {
            RegisterPCValue = _pc.Output,
            RegisterOldPCValue = _oldPC.Output,
            RegisterInstrValue = _instructionReg.Output,
            RegisterDataValue = _dataReg.Output,
            RegisterScrAValue = _srcA.Output,
            RegisterSrcBValue = _srcB.Output,
            RegisterAluOutValue = _aluOutReg.Output,

            RegisterFieldValue = (int[])_registerFile.Registers.Clone(),

            FirstMemoryValue = _dataInstructionMemory.Read(0),
            SecondMemoryValue = _dataInstructionMemory.Read(4),
            ThirdMemoryValue = _dataInstructionMemory.Read(8),
            FourthMemoryValue = _dataInstructionMemory.Read(12),

            RegisterPCwe = _pc.WriteEnable,
            RegisterOldPCwe = _oldPC.WriteEnable,
            RegisterInstrWe = _instructionReg.WriteEnable,
            RegisterDataWe = _dataReg.WriteEnable,
            RegisterScrAwe = _srcA.WriteEnable,
            RegisterSrcBwe = _srcB.WriteEnable,
            RegisterAluOutWe = _aluOutReg.WriteEnable,

            AluOperation = aluVisualizer.CurrentAluOperation,

            ExtenderOperation = extenderVisualizer.CurrentAluOperation,

            MuXadrPath = adrMuxVisualizer.CurrentChosenMuxPath,
            MuXsrcAPath = srcAmuxVisualizer.CurrentChosenMuxPath,
            MuXsrcBPath = srcBmuxVisualizer.CurrentChosenMuxPath,
            MuXresultPath = resultMuxVisualizer.CurrentChosenMuxPath,
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _pc.WriteEnable = registerPCVisualizer.isWriteEnabled;
        _oldPC.WriteEnable = registerOldPCVisualizer.isWriteEnabled;
        _instructionReg.WriteEnable = registerInstructionVisualizer.isWriteEnabled;
        _dataReg.WriteEnable = registerDataVisualizer.isWriteEnabled;
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _aluOutReg.WriteEnable = registerAluOutVisualizer.isWriteEnabled;
        _dataInstructionMemory.WriteEnable = memoryVisualizer.isWriteEnabled;
        _registerFile.WriteEnable = registerFileVisualizer.isWriteEnabled;

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
    private int ImmExt => Extender.Evaluate((ImmediateType)extenderVisualizer.CurrentAluOperation, (uint)_instructionReg.Output);

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

    protected override void UpdateVisualizers()
    {
        UpdateSidePanel();

        _infoPCRegister.Display("PC Register", _pc.Output);
        _infoOldPCRegister.Display("Old PC Register", _oldPC.Output);
        _infoInstructionRegister.Display("Instruction Register", RiscVDecoder.CommandBuilder((uint)_instructionReg.Output));
        _infoDataRegister.Display("Data Register", _dataReg.Output);
        _infoSrcARegister.Display("SrcA Register", _srcA.Output);
        _infoSrcBRegister.Display("SrcB Register", _srcB.Output);
        _infoAluOutRegister.Display("ALU Out Register", _aluOutReg.Output);

        memoryVisualizer.UIRegisterPanel.Display(
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(0)),
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(4)),
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(8)),
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(12))
        );
        registerFileVisualizer.UIRegisterPanel.Display(_registerFile.Registers);


        // ==============================  WE SECTION  =====================================
        registerPCVisualizer.ForceUpdateWriteEnableVisualization(_pc.WriteEnable);
        registerOldPCVisualizer.ForceUpdateWriteEnableVisualization(_oldPC.WriteEnable);
        registerInstructionVisualizer.ForceUpdateWriteEnableVisualization(_instructionReg.WriteEnable);
        registerDataVisualizer.ForceUpdateWriteEnableVisualization(_dataReg.WriteEnable);
        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerAluOutVisualizer.ForceUpdateWriteEnableVisualization(_aluOutReg.WriteEnable);

        registerFileVisualizer.ForceUpdateWriteEnableVisualization(_registerFile.WriteEnable);
        memoryVisualizer.ForceUpdateWriteEnableVisualization(_dataInstructionMemory.WriteEnable);
    }


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
