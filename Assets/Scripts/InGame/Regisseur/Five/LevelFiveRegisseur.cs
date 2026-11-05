using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelFiveState
{
    public int RegisterPCValue;
    public int RegisterInstrValue;
    public int RegisterOutputValue;

    public int FirstMemoryValue;
    public int SecondMemoryValue;
    public int ThirdMemoryValue;
    public int FourthMemoryValue;

    public bool RegisterPCwe;
    public bool RegisterInstrWe;
    public bool RegisterOutputWe;

    public int AluOperation;

    public int ExtenderOperation;
}

[Serializable]
public class LevelFiveBusSegments: IBusSegmentProvider
{
    [Header("Fetch")] [Tooltip("PC -> Memory address")]
    public LineRenderer pcToMemAddr;

    [Tooltip("PC -> BTA adder input A")] public LineRenderer pcToBtaAdder;

    [Tooltip("Constant 4 -> PC+4 adder input B")]
    public LineRenderer constFourToAdder;

    [Tooltip("Memory read data -> Instruction register")]
    public LineRenderer memDataToInstrReg;

    [Tooltip("PC+4 adder result -> SrcB register")]
    public LineRenderer adderToSrcB;

    [Header("Decode")] [Tooltip("Instruction register output -> Extend unit")]
    public LineRenderer instrRegToExtend;

    [Tooltip("Extend unit output -> downstream MUX/register")]
    public LineRenderer extendToMux;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(pcToMemAddr);
        c.RegisterSegment(pcToBtaAdder);
        c.RegisterSegment(constFourToAdder);
        c.RegisterSegment(memDataToInstrReg);
        c.RegisterSegment(adderToSrcB);
        c.RegisterSegment(instrRegToExtend);
        c.RegisterSegment(extendToMux);
    }
}

public class LevelFiveRegisseur : BaseLevelRegisseur<LevelFiveState, LevelFiveBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")] [Header("Level 5 Specific Components")] [SerializeField]
    private RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField]
    private RegisterVisualizer registerSrcBVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    private RegisterVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("_memoryVisualizer")] [SerializeField]
    private InstructionDataMemoryVisualizer memoryVisualizer;

    [FormerlySerializedAs("_aluVizualizer")] [SerializeField]
    private AluVisualiser aluVisualizer;

    [FormerlySerializedAs("extenderVizualizer")] [FormerlySerializedAs("_extenderVizualizer")] [SerializeField]
    private ExtenderVisualizer extenderVisualizer;

    [FormerlySerializedAs("_blinkerNumber")] [SerializeField]
    private Blinker blinkerNumber;


    private int _currentBus; // [0, 6]
    private DataInstMemory _dataInstructionMemory;
    private Register _output;

    // Intern components for computations
    private Register _srcA;
    private Register _srcB;

    protected override int RightAnswerValue => 66;

    protected override void OnLevelStart()
    {
        // Initialization of logical components
        _srcA = new Register();
        _srcB = new Register();
        _output = new Register();

        _dataInstructionMemory = new DataInstMemory();

        _dataInstructionMemory.LoadWords(
            1048576239,  // J-type (1000)
            4314211,     // B-type (16)
            7603235,     // S-type (8)
            4301059);    // I-type (4)

        Clock.Add(_srcA, _srcB, _output, _dataInstructionMemory);

        // Caching of UI panels for visualizers
        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;
        _infoOutputRegister = registerOutputVisualizer.UIRegisterPanel;

        memoryVisualizer.UIRegisterPanel.Display(_dataInstructionMemory.Read(0),
            _dataInstructionMemory.Read(4), _dataInstructionMemory.Read(8),
            _dataInstructionMemory.Read(12));
        UpdateVisualizers();
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var pc = _srcA.Output;

            yield return Bus.Wave((buses.pcToMemAddr, pc), (buses.pcToBtaAdder, pc), (buses.constFourToAdder, 4));
            yield return Bus.Wave(
                (buses.memDataToInstrReg, _dataInstructionMemory.Read(pc)),
                (buses.adderToSrcB, Alu.Calculate(pc, 4, aluVisualizer.CurrentAluOperation)));
            yield return Bus.Wave((buses.instrRegToExtend, _srcB.Output));
            yield return Bus.Wave((buses.extendToMux, Extender.Evaluate(extenderVisualizer.CurrentAluOperation, (uint)_srcB.Output)));

            _currentBus++;
        }
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            var before = StateAt(TickCounter);

            yield return Bus.WaveBack((buses.extendToMux, _output.Input));
            yield return Bus.WaveBack((buses.instrRegToExtend, before.RegisterInstrValue));
            yield return Bus.WaveBack((buses.memDataToInstrReg, _srcB.Input), (buses.adderToSrcB, _srcA.Input));
            yield return Bus.WaveBack((buses.pcToMemAddr, _srcA.Output), (buses.pcToBtaAdder, _srcA.Output), (buses.constFourToAdder, 4));

            _currentBus--;
        }
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _output.WriteEnable = registerOutputVisualizer.isWriteEnabled;
        _dataInstructionMemory.WriteEnable = registerOutputVisualizer.isWriteEnabled;

        // implementation
        _srcB.Input = _dataInstructionMemory.Read(_srcA.Output);

        _srcA.Input = Alu.Calculate(_srcA.Output, 4, aluVisualizer.CurrentAluOperation);

        _output.Input = Extender.Evaluate(extenderVisualizer.CurrentAluOperation, (uint)_srcB.Output);

        switch (_currentBus)
        {
            case 2:
                _dataInstructionMemory.LoadWord(0, _output.Output);
                break;
            case 3:
                _dataInstructionMemory.LoadWord(4, _output.Output);
                break;
            case 4:
                _dataInstructionMemory.LoadWord(8, _output.Output);
                break;
            case 5:
                _dataInstructionMemory.LoadWord(12, _output.Output);
                break;
        }

        Clock.Tick();
    }

    protected override LevelFiveState GetCurrentState()
    {
        return new LevelFiveState
        {
            RegisterPCValue = _srcA.Output,
            RegisterInstrValue = _srcB.Output,
            RegisterOutputValue = _output.Output,

            FirstMemoryValue = _dataInstructionMemory.Read(0),
            SecondMemoryValue = _dataInstructionMemory.Read(4),
            ThirdMemoryValue = _dataInstructionMemory.Read(8),
            FourthMemoryValue = _dataInstructionMemory.Read(12),

            RegisterPCwe = _srcA.WriteEnable,
            RegisterInstrWe = _srcB.WriteEnable,
            RegisterOutputWe = _output.WriteEnable,


            AluOperation = aluVisualizer.CurrentAluOperation,

            ExtenderOperation = extenderVisualizer.CurrentAluOperation
        };
    }

    protected override void ApplyState(LevelFiveState s)
    {
        _srcA.Reset(s.RegisterPCValue);
        _srcB.Reset(s.RegisterInstrValue);
        _output.Reset(s.RegisterOutputValue);

        _dataInstructionMemory.LoadWords(s.FirstMemoryValue, s.SecondMemoryValue, s.ThirdMemoryValue, s.FourthMemoryValue);

        _srcA.WriteEnable = s.RegisterPCwe;
        _srcB.WriteEnable = s.RegisterInstrWe;
        _output.WriteEnable = s.RegisterOutputWe;


        aluVisualizer.ChooseAluOperation(s.AluOperation);
        extenderVisualizer.ChooseAluOperation(s.ExtenderOperation);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerOutputVisualizer.TriggerBlink();
        memoryVisualizer.TriggerBlink();
        blinkerNumber.Trigger();
    }

    protected override bool CheckWinCondition()
    {
        var val1 = (uint)StateAt(2).RegisterOutputValue;
        var val2 = (uint)StateAt(3).RegisterOutputValue;
        var val3 = (uint)StateAt(4).RegisterOutputValue;
        var val4 = (uint)_output.Output;

        return val1 == 1000 &&
               val2 == 8 &&
               val3 == 8 &&
               val4 == 4;
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register 1", _srcA.Output);
        _infoSrcBRegister.Display("Register 2", RiscVDecoder.CommandBuilder((uint)_srcB.Output));
        _infoOutputRegister.Display("Register 3", _output.Output);
        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerOutputVisualizer.ForceUpdateWriteEnableVisualization(_output.WriteEnable);


        memoryVisualizer.UIRegisterPanel.Display(
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(0)),
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(4)),
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(8)),
            RiscVDecoder.CommandBuilder((uint)_dataInstructionMemory.Read(12))
        );
        memoryVisualizer.ForceUpdateWriteEnableVisualization(_dataInstructionMemory.WriteEnable);
    }

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;
    private InfoPanelUI _infoOutputRegister;

    #endregion
}