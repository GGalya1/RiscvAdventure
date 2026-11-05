using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class LevelFourthBusSegments: IBusSegmentProvider
{
    [Header("PC fanout")] [Tooltip("PC (_srcA) -> Memory address input")]
    public LineRenderer pcToMemAddr;

    [Tooltip("PC (_srcA) -> ADR MUX / BTA path")]
    public LineRenderer pcToAdrMux;

    [FormerlySerializedAs("memDataTo_srcB")] [Header("Memory")] [Tooltip("Memory read data -> _srcB register")]
    public LineRenderer memDataToSrcB;

    [FormerlySerializedAs("_srcBToExt")] [Tooltip("_srcB register output -> downstream (Extend)")]
    public LineRenderer srcBToExt;

    [FormerlySerializedAs("_srcBToDataMemWd")] [Tooltip("_srcB output -> Data Memory write data (WD)")]
    public LineRenderer srcBToDataMemWd;

    [Header("PC+4 adder")] [Tooltip("MUX-selected value -> PC+4 adder input A")]
    public LineRenderer muxToAdder;

    [Tooltip("Constant 4 -> PC+4 adder input B")]
    public LineRenderer constFourToAdder;

    [FormerlySerializedAs("adderTo_srcA")] [Tooltip("PC+4 adder result -> _srcA register input")]
    public LineRenderer adderToSrcA;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(pcToMemAddr);
        c.RegisterSegment(pcToAdrMux);
        c.RegisterSegment(memDataToSrcB);
        c.RegisterSegment(srcBToExt);
        c.RegisterSegment(srcBToDataMemWd);
        c.RegisterSegment(muxToAdder);
        c.RegisterSegment(constFourToAdder);
        c.RegisterSegment(adderToSrcA);
    }
}

public class LevelFourthRegisseur : BaseLevelRegisseur<LevelThreeState, LevelFourthBusSegments>
{
    [Header("Level 4 Specific Components")] [SerializeField]
    private MultiplexerVisualizer multiplexerVisualizer;

    [SerializeField] private RegisterVisualizer registerSrcAVisualizer;
    [SerializeField] private RegisterVisualizer registerSrcBVisualizer;
    [SerializeField] private InstructionDataMemoryVisualizer registerOutputVisualizer;
    [SerializeField] private AluVisualiser aluVisualizer;
    [SerializeField] private Blinker numberBlinker;

    private int _currentBus;
    private DataInstMemory _dataInstructionMemory;
    private InstrMemoryControlPanel _infoDataMemory;

    private InfoPanelUI _infoSrcA;
    private InfoPanelUI _infoSrcB;

    private Register _srcA;
    private Register _srcB;

    protected override void OnLevelStart()
    {
        // Initialization of logical components
        _srcA = new Register();
        _srcB = new Register(8);
        _dataInstructionMemory = new DataInstMemory();
        _dataInstructionMemory.LoadWord(0, 0);
        _dataInstructionMemory.LoadWord(4, 3);
        _dataInstructionMemory.LoadWord(8, -256);
        _dataInstructionMemory.LoadWord(12, -1024);

        // Caching of UI panels for visualizers
        _infoSrcA = registerSrcAVisualizer.UIRegisterPanel;
        _infoSrcB = registerSrcBVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override bool CheckWinCondition()
    {
        return _dataInstructionMemory.Memory[0] < _dataInstructionMemory.Memory[correctAnswer];
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizer and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _srcB.WriteEnable = registerSrcBVisualizer.isWriteEnabled;
        _dataInstructionMemory.WriteEnable = registerOutputVisualizer.isWriteEnabled;

        // implementation
        _srcB.Input = _dataInstructionMemory.Memory.GetValueOrDefault(_srcA.Output, 0);

        _dataInstructionMemory.Address = _srcA.Output;
        _dataInstructionMemory.WriteData = _srcB.Output;

        var p = multiplexerVisualizer.CurrentChosenMuxPath;
        if (p == -1)
        {
            CustomLog.LogEditorError("MUX path is -1. No value will be propagated");
            _srcA.Input = 0;
        }
        else if (p == 0)
        {
            _srcA.Input = Alu.Calculate(_srcA.Output, 4, aluVisualizer.CurrentAluOperation);
        }
        else if (p == 1)
        {
            _srcA.Input = Alu.Calculate(_srcB.Output, 4, aluVisualizer.CurrentAluOperation);
        }
        else
        {
            CustomLog.LogEditorError($"MUX path is incorrect! Expected [-1, 1] but got {p}");
            _srcA.Input = 0;
        }


        _srcA.PreClockUpdate();
        _srcB.PreClockUpdate();
        _dataInstructionMemory.PreClockUpdate();


        // Only if WriteEnable = true, call Clock
        _srcA.Clock();
        _srcB.Clock();
        _dataInstructionMemory.Clock();
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var pc = _srcA.Output;

            yield return Bus.Wave((buses.pcToMemAddr, pc), (buses.pcToAdrMux, pc));
            yield return Bus.Wave((buses.memDataToSrcB, _dataInstructionMemory.Memory.GetValueOrDefault(pc, 0)));
            yield return Bus.Wave((buses.srcBToExt, _srcB.Output), (buses.srcBToDataMemWd, _srcB.Output));

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
            yield return Bus.Wave((buses.adderToSrcA, Alu.Calculate(propagationVal, 4, aluVisualizer.CurrentAluOperation)));

            _currentBus++;
        }
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
            yield return Bus.WaveBack((buses.muxToAdder, upperBusSignal), (buses.constFourToAdder, 4));
            yield return Bus.WaveBack(
                (buses.srcBToDataMemWd, before.RegisterInstrValue), (buses.srcBToExt, before.RegisterInstrValue));
            yield return Bus.WaveBack((buses.pcToAdrMux, before.RegisterPCValue), (buses.memDataToSrcB, _srcB.Input));
            yield return Bus.WaveBack((buses.pcToMemAddr, before.RegisterPCValue));

            _currentBus--;
        }
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
            InstrDataMemoryWe = _dataInstructionMemory.WriteEnable,

            CurrentChosenMuxPath = multiplexerVisualizer.CurrentChosenMuxPath,
            AluOperation = aluVisualizer.CurrentAluOperation
        };
    }

    protected override void ApplyState(LevelThreeState s)
    {
        _srcA.Reset(s.RegisterPCValue);
        _srcB.Reset(s.RegisterInstrValue);

        _dataInstructionMemory.LoadWord(0, s.FirstMemoryValue);
        _dataInstructionMemory.LoadWord(4, s.SecondMemoryValue);
        _dataInstructionMemory.LoadWord(8, s.ThirdMemoryValue);
        _dataInstructionMemory.LoadWord(12, s.FourthMemoryValue);

        _srcA.WriteEnable = s.RegisterPcwe;
        _srcB.WriteEnable = s.RegisterInstrWe;
        _dataInstructionMemory.WriteEnable = s.InstrDataMemoryWe;

        ApplyMuxState(s.CurrentChosenMuxPath, multiplexerVisualizer);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerSrcBVisualizer.TriggerBlink();
        registerOutputVisualizer.TriggerBlink();
        numberBlinker.Trigger();
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcA.Display("Register 1", _srcA.Output);
        _infoSrcB.Display("Register 2", _srcB.Output);
        registerOutputVisualizer.UIRegisterPanel.Display(
            _dataInstructionMemory.Memory[0],
            _dataInstructionMemory.Memory[4],
            _dataInstructionMemory.Memory[8],
            _dataInstructionMemory.Memory[12]
        );

        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerSrcBVisualizer.ForceUpdateWriteEnableVisualization(_srcB.WriteEnable);
        registerOutputVisualizer.ForceUpdateWriteEnableVisualization(_dataInstructionMemory.WriteEnable);
    }
}