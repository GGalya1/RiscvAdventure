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

    public int AluOperation;
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

    protected override void OnLevelStart()
    {
        // Initialization of logical components
        _srcA = new Register(srcAValue)
        {
            WriteEnable = true
        };
        _srcB = new Register(srcBValue)
        {
            WriteEnable = true
        };
        _dataInstructionMemory = new DataInstMemory
        {
            MemoryWrite = true
        };
        _dataInstructionMemory.LoadWord(0, 256);
        _dataInstructionMemory.LoadWord(4, 128);
        _dataInstructionMemory.LoadWord(8, -89);
        _dataInstructionMemory.LoadWord(12, 66);

        // Caching of UI panels for visualizers
        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcBRegister = registerSrcBVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override void ApplyState(LevelThreeState s)
    {
        _srcA.Reset(s.RegisterPCValue);
        _srcB.Reset(s.RegisterInstrValue);
        
        _dataInstructionMemory.MemoryWrite = s.InstrDataMemoryWe;
        _dataInstructionMemory.LoadWord(0, s.FirstMemoryValue);
        _dataInstructionMemory.LoadWord(4, s.SecondMemoryValue);
        _dataInstructionMemory.LoadWord(8, s.ThirdMemoryValue);
        _dataInstructionMemory.LoadWord(12, s.FourthMemoryValue);

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

            FirstMemoryValue = _dataInstructionMemory.Memory[0],
            SecondMemoryValue = _dataInstructionMemory.Memory[4],
            ThirdMemoryValue = _dataInstructionMemory.Memory[8],
            FourthMemoryValue = _dataInstructionMemory.Memory[12],

            RegisterPcwe = _srcA.WriteEnable,
            RegisterInstrWe = _srcB.WriteEnable,
            InstrDataMemoryWe = _dataInstructionMemory.MemoryWrite,

            CurrentChosenMuxPath = multiplexerVisualizer.CurrentChosenMuxPath,
            AluOperation = aluVisualizer.CurrentAluOperation
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _dataInstructionMemory.MemoryWrite = registerOutputVisualizer.isWriteEnabled;

        // implementation
        _srcB.Input = _dataInstructionMemory.Memory.GetValueOrDefault(_srcA.Output, 0);


        var p = multiplexerVisualizer.CurrentChosenMuxPath;
        switch (p)
        {
            case -1:
                CustomLog.LogEditorError("MUX path is -1. No value will be propagated");
                _srcA.Input = 0;
                break;
            case 0:
                _srcA.Input = Alu.Calculate(_srcA.Output, 4, aluVisualizer.CurrentAluOperation);
                break;
            case 1:
                _srcA.Input = Alu.Calculate(_srcB.Output, 4, aluVisualizer.CurrentAluOperation);
                break;
            default:
                CustomLog.LogEditorError($"MUX path is incorrect! Expected [-1, 1] but got {p}");
                _srcA.Input = 0;
                break;
        }


        _srcA.PreClockUpdate();
        _srcB.PreClockUpdate();
        _dataInstructionMemory.PreClockUpdate();


        // Only if WriteEnable = true, call Clock
        _srcA.Clock();
        _srcB.Clock();
        _dataInstructionMemory.Clock();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            busController.StartBusSignal(buses.adderToSrcA, _srcA.Input, true);


            var upperBusSignal = 0;
            if (multiplexerVisualizer.CurrentChosenMuxPath == 0)
                upperBusSignal = StateAt(TickCounter).RegisterPCValue;
            else if (multiplexerVisualizer.CurrentChosenMuxPath == 1)
                upperBusSignal = StateAt(TickCounter).RegisterInstrValue;
            yield return StartCoroutine(DelayedSignals(buses.adderToSrcA, upperBusSignal, buses.constFourToAdder, 4,
                true, true));

            yield return StartCoroutine(DelayedSignals(buses.pcToAdrMux, StateAt(TickCounter).RegisterPCValue,
                buses.srcBToExt, StateAt(TickCounter).RegisterInstrValue, true, true));


            yield return StartCoroutine(DelayedSignal(buses.memDataToSrcB, _srcB.Input, true));


            yield return StartCoroutine(DelayedSignal(buses.pcToMemAddr, StateAt(TickCounter).RegisterPCValue,
                true));


            _currentBus--;
        }

        yield return WaitNoSignals;
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            busController.StartBusSignal(buses.pcToMemAddr, _srcA.Output);
            busController.StartBusSignal(buses.pcToAdrMux, _srcA.Output);

            // should be by a short divisor
            if (_dataInstructionMemory.Memory.TryGetValue(_srcA.Output, out var value))
                yield return StartCoroutine(DelayedSignal(buses.memDataToSrcB, value));
            else
                yield return StartCoroutine(DelayedSignal(buses.memDataToSrcB, 0));


            // should follow the first one with a short division
            yield return StartCoroutine(DelayedSignal(buses.srcBToExt, _srcB.Output));

            var propagationVal = 0;
            if (multiplexerVisualizer.CurrentChosenMuxPath == -1)
            {
                yield return StartCoroutine(DelayedSignal(buses.constFourToAdder, 0));
            }
            else
            {
                if (multiplexerVisualizer.CurrentChosenMuxPath == 0)
                    propagationVal = _srcA.Output;
                else if (multiplexerVisualizer.CurrentChosenMuxPath == 1)
                    propagationVal = _srcB.Output;
                else
                    CustomLog.LogEditorError($"Unexpected MUX path {multiplexerVisualizer.CurrentChosenMuxPath}");

                yield return StartCoroutine(DelayedSignals(buses.muxToAdder, propagationVal, buses.constFourToAdder,
                    4));
            }


            // from ALU to first register
            yield return StartCoroutine(DelayedSignal(buses.adderToSrcA,
                Alu.Calculate(propagationVal, 4, aluVisualizer.CurrentAluOperation)));

            _currentBus++;
        }

        yield return WaitNoSignals;
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register 1", _srcA.Output);
        _infoSrcBRegister.Display("Register 2", _srcB.Output);
        registerOutputVisualizer.UIRegisterPanel.Display(_dataInstructionMemory.Memory[0],
            _dataInstructionMemory.Memory[4], _dataInstructionMemory.Memory[8],
            _dataInstructionMemory.Memory[12]);

        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerOutputVisualizer.ForceUpdateWriteEnableVisualization(_dataInstructionMemory.MemoryWrite);
    }

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoSrcBRegister;

    #endregion
}