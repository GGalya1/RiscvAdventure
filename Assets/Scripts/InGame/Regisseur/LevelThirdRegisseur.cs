using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelThreeState
{
    public int RegisterPCValue;
    public int RegisterInstrValue;

    public int FirstMemoryValue;
    public int SecondMemoryValue;
    public int ThirdMemoryValue;
    public int FourthMemoryValue;

    public bool RegisterPcwe;
    public bool RegisterInstrWe;
    public bool InstrDataMemoryWe;

    public int
        CurrentChosenMuxPath; // since we can call ResetVisualization and, based on the selected path, call one of the Visualizer methods

    public AluOperation AluOperation;
}

[Serializable]
public class LevelThirdBusSegments: IBusSegmentProvider
{
    [Header("PC fanout")] [Tooltip("PC (SrcA) -> Memory address input")]
    public LineRenderer pcToMemAddr;

    [Tooltip("PC (SrcA) -> ADR MUX / BTA path")]
    public LineRenderer pcToAdrMux;

    [Header("Memory")] [Tooltip("Memory read data -> SrcB register")]
    public LineRenderer memDataToSrcB;

    [Tooltip("SrcB register output (instruction) -> downstream")]
    public LineRenderer srcBToExt;

    [Header("PC+4 adder")] [Tooltip("MUX-selected value -> PC+4 adder input A")]
    public LineRenderer muxToAdder;

    [Tooltip("Constant 4 -> PC+4 adder input B")]
    public LineRenderer constFourToAdder;

    [Tooltip("PC+4 adder result -> SrcA register input")]
    public LineRenderer adderToSrcA;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(pcToMemAddr);
        c.RegisterSegment(pcToAdrMux);
        c.RegisterSegment(memDataToSrcB);
        c.RegisterSegment(srcBToExt);
        c.RegisterSegment(muxToAdder);
        c.RegisterSegment(constFourToAdder);
        c.RegisterSegment(adderToSrcA);
    }
}

public class LevelThirdRegisseur : BaseLevelRegisseur<LevelThreeState, LevelThirdBusSegments>
{
    [FormerlySerializedAs("_multiplexerVisualizer")] [Header("Level 3 Specific Components")] [SerializeField]
    private MultiplexerVisualizer multiplexerVisualizer;

    [FormerlySerializedAs("_registerSrcAVisualizer")] [SerializeField]
    private RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField]
    private RegisterVisualizer registerSrcBVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    private InstructionDataMemoryVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("aluVizualizer")] [FormerlySerializedAs("_aluVizualizer")] [SerializeField]
    private AluVisualiser aluVisualizer;

    [SerializeField] private int srcAValue = 5;
    [SerializeField] private int srcBValue = 7;

    [FormerlySerializedAs("_numberBlinker")] [SerializeField]
    private Blinker numberBlinker;


    private int _currentBus; // [0, 5]
    private DataInstMemory _dataInstructionMemory;

    // Intern components for computations
    private Register _srcA;
    private Register _srcB;

    private readonly Alu _alu = new();

    protected override void OnLevelStart()
    {
        aluVisualizer.Bind(_alu);

        // Initialization of logical components
        _srcA = new Register(srcAValue);
        _srcB = new Register(srcBValue);
        _dataInstructionMemory = new DataInstMemory();
        _dataInstructionMemory.LoadWords(256, 128, -89, 66);

        Clock.Add(_srcA, _srcB, _dataInstructionMemory);

        // Caching of UI panels for visualizers
        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override void ApplyState(LevelThreeState s)
    {
        _srcA.Reset(s.RegisterPCValue);
        _srcB.Reset(s.RegisterInstrValue);
        
        _dataInstructionMemory.WriteEnable = s.InstrDataMemoryWe;
        _dataInstructionMemory.LoadWords(s.FirstMemoryValue, s.SecondMemoryValue, s.ThirdMemoryValue, s.FourthMemoryValue);

        _srcA.WriteEnable = s.RegisterPcwe;
        _srcB.WriteEnable = s.RegisterInstrWe;

        ApplyMuxState(s.CurrentChosenMuxPath, multiplexerVisualizer);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerOutputVisualizer.TriggerBlink();
        numberBlinker.Trigger();
    }

    protected override bool CheckWinCondition()
    {
        return _srcB.Output == RightAnswerValue;
    }

    protected override LevelThreeState GetCurrentState()
    {
        return new LevelThreeState
        {
            RegisterPCValue = _srcA.Output,
            RegisterInstrValue = _srcB.Output,

            FirstMemoryValue = _dataInstructionMemory.Read(0),
            SecondMemoryValue = _dataInstructionMemory.Read(4),
            ThirdMemoryValue = _dataInstructionMemory.Read(8),
            FourthMemoryValue = _dataInstructionMemory.Read(12),

            RegisterPcwe = _srcA.WriteEnable,
            RegisterInstrWe = _srcB.WriteEnable,
            InstrDataMemoryWe = _dataInstructionMemory.WriteEnable,

            CurrentChosenMuxPath = multiplexerVisualizer.CurrentChosenMuxPath,
            AluOperation = _alu.Operation
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _dataInstructionMemory.WriteEnable = registerOutputVisualizer.isWriteEnabled;

        // implementation
        _srcB.Input = _dataInstructionMemory.Read(_srcA.Output);


        var p = multiplexerVisualizer.CurrentChosenMuxPath;
        switch (p)
        {
            case -1:
                CustomLog.LogEditorError("MUX path is -1. No value will be propagated");
                _srcA.Input = 0;
                break;
            case 0:
                _srcA.Input = _alu.Compute(_srcA.Output, 4);
                break;
            case 1:
                _srcA.Input = _alu.Compute(_srcB.Output, 4);
                break;
            default:
                CustomLog.LogEditorError($"MUX path is incorrect! Expected [-1, 1] but got {p}");
                _srcA.Input = 0;
                break;
        }

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            var before = StateAt(TickCounter);
            var upperBusSignal = 0;
            if (multiplexerVisualizer.CurrentChosenMuxPath == 0)
                upperBusSignal = before.RegisterPCValue;
            else if (multiplexerVisualizer.CurrentChosenMuxPath == 1)
                upperBusSignal = before.RegisterInstrValue;

            yield return Bus.WaveBack((buses.adderToSrcA, _srcA.Input));
            yield return Bus.WaveBack((buses.adderToSrcA, upperBusSignal), (buses.constFourToAdder, 4));
            yield return Bus.WaveBack((buses.pcToAdrMux, before.RegisterPCValue), (buses.srcBToExt, before.RegisterInstrValue));
            yield return Bus.WaveBack((buses.memDataToSrcB, _srcB.Input));
            yield return Bus.WaveBack((buses.pcToMemAddr, before.RegisterPCValue));

            _currentBus--;
        }
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var pc = _srcA.Output;

            yield return Bus.Wave((buses.pcToMemAddr, pc), (buses.pcToAdrMux, pc));
            yield return Bus.Wave((buses.memDataToSrcB, _dataInstructionMemory.Read(pc)));
            yield return Bus.Wave((buses.srcBToExt, _srcB.Output));

            var propagationVal = 0;
            if (multiplexerVisualizer.CurrentChosenMuxPath == -1)
            {
                yield return Bus.Wave((buses.constFourToAdder, 0));
            }
            else
            {
                if (multiplexerVisualizer.CurrentChosenMuxPath == 0)
                    propagationVal = _srcA.Output;
                else if (multiplexerVisualizer.CurrentChosenMuxPath == 1)
                    propagationVal = _srcB.Output;
                else
                    CustomLog.LogEditorError($"Unexpected MUX path {multiplexerVisualizer.CurrentChosenMuxPath}");

                yield return Bus.Wave((buses.muxToAdder, propagationVal), (buses.constFourToAdder, 4));
            }

            // from ALU to first register
            yield return Bus.Wave((buses.adderToSrcA, _alu.Compute(propagationVal, 4)));

            _currentBus++;
        }
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register 1", _srcA.Output);
        _infoSrcBRegister.Display("Register 2", _srcB.Output);
        registerOutputVisualizer.UIRegisterPanel.Display(_dataInstructionMemory.Read(0),
            _dataInstructionMemory.Read(4), _dataInstructionMemory.Read(8),
            _dataInstructionMemory.Read(12));

        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerOutputVisualizer.ForceUpdateWriteEnableVisualization(_dataInstructionMemory.WriteEnable);
    }

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;

    #endregion
}