using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct ExtendedFirstLevelState
{
    public int RegisterOutputValue;

    public int MuXup;
    public int MuXmiddle;
    public int MuXdown;
    public int MuXoutput;
}

[Serializable]
public class LevelOneExtendedBusSegments: IBusSegmentProvider
{
    [Header("Middle MUX inputs (constants 8 and 12)")] [Tooltip("Constant 8 -> Middle MUX input [0]")]
    public LineRenderer constAToMiddleMux;

    [Tooltip("Constant 12 -> Middle MUX input [1]")]
    public LineRenderer constBToMiddleMux;

    [Header("Down MUX inputs")] [Tooltip("Middle MUX output -> Down MUX input [0]")]
    public LineRenderer middleMuxToDownMux;

    [Tooltip("Constant -8 -> Down MUX input [1]")]
    public LineRenderer constCToDownMux;

    [Tooltip("Constant -12 -> Down MUX input [2]")]
    public LineRenderer constDToDownMux;

    [Header("Upper MUX inputs (constants -4 and 0)")] [Tooltip("Constant -4 -> Upper MUX input [0]")]
    public LineRenderer constEToUpperMux;

    [Tooltip("Constant 0 -> Upper MUX input [1]")]
    public LineRenderer constFToUpperMux;

    [Header("Output MUX inputs")] [Tooltip("Upper MUX output -> Output MUX input [0]")]
    public LineRenderer upperMuxToOutputMux;

    [Tooltip("Down MUX output -> Output MUX input [2]")]
    public LineRenderer downMuxToOutputMux;

    [Tooltip("Constant 4 -> Output MUX input [1]")]
    public LineRenderer constGToOutputMux;

    [Header("Output")] [Tooltip("Output MUX -> Output register")]
    public LineRenderer outputMuxToRegister;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(constAToMiddleMux);
        c.RegisterSegment(constBToMiddleMux);
        c.RegisterSegment(middleMuxToDownMux);
        c.RegisterSegment(constCToDownMux);
        c.RegisterSegment(constDToDownMux);
        c.RegisterSegment(constEToUpperMux);
        c.RegisterSegment(constFToUpperMux);
        c.RegisterSegment(upperMuxToOutputMux);
        c.RegisterSegment(downMuxToOutputMux);
        c.RegisterSegment(constGToOutputMux);
        c.RegisterSegment(outputMuxToRegister);
    }
}

public class LevelOneExtended : BaseLevelRegisseur<ExtendedFirstLevelState, LevelOneExtendedBusSegments>
{
    [FormerlySerializedAs("_registerOutputVisualizer")] [Header("MUXes specific components")] [SerializeField]
    private RegisterVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("_upperMUXVisualizer")] [SerializeField]
    private MultiplexerVisualizer upperMuxVisualizer;

    [FormerlySerializedAs("_middleMUXVisualizer")] [SerializeField]
    private MultiplexerVisualizer middleMuxVisualizer;

    [FormerlySerializedAs("_downMUXVisualizer")] [SerializeField]
    private MultiplexerVisualizer downMuxVisualizer;

    [FormerlySerializedAs("_outputMUXVisualizer")] [SerializeField]
    private MultiplexerVisualizer outputMuxVisualizer;

    [FormerlySerializedAs("_numberBlinkers")] [SerializeField]
    private Blinker[] numberBlinkers;

    private int _currentBus; // [0, 2]
    private InfoPanelUI _infoOutputRegister;

    private Register _output;

    protected override int RightAnswerValue => 12;

    protected override void OnLevelStart()
    {
        _infoOutputRegister = registerOutputVisualizer.UIRegisterPanel;
        _output = new Register(11)
        {
            WriteEnable = true
        };

        UpdateVisualizers();
    }

    protected override void ApplyState(ExtendedFirstLevelState s) // "s" for state
    {
        _output.Reset(s.RegisterOutputValue);

        ApplyMuxState(s.MuXup, upperMuxVisualizer);
        ApplyMuxState(s.MuXmiddle, middleMuxVisualizer);
        ApplyMuxState(s.MuXdown, downMuxVisualizer);
        ApplyMuxState(s.MuXoutput, outputMuxVisualizer);
    }

    protected override void BlinkClockedComponents()
    {
        foreach (var b in numberBlinkers) b.Trigger();
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override ExtendedFirstLevelState GetCurrentState()
    {
        return new ExtendedFirstLevelState
        {
            RegisterOutputValue = _output.Output,

            MuXup = upperMuxVisualizer.CurrentChosenMuxPath,
            MuXmiddle = middleMuxVisualizer.CurrentChosenMuxPath,
            MuXdown = downMuxVisualizer.CurrentChosenMuxPath,
            MuXoutput = outputMuxVisualizer.CurrentChosenMuxPath
        };
    }

    protected override void HandleClockUpdate()
    {
        var up = EvaluateMux(upperMuxVisualizer.CurrentChosenMuxPath, -4, 0, -1);
        var left = EvaluateMux(middleMuxVisualizer.CurrentChosenMuxPath, 8, 12, -1);
        var down = EvaluateMux(downMuxVisualizer.CurrentChosenMuxPath, left, -8, -12);

        _output.Input = EvaluateMux(outputMuxVisualizer.CurrentChosenMuxPath, up, 4, down);
        _output.WriteEnable = registerOutputVisualizer.isWriteEnabled;

        _output.PreClockUpdate();
        _output.Clock();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            var up = upperMuxVisualizer.CurrentChosenMuxPath == 0 ? -4 : 0;
            var left = middleMuxVisualizer.CurrentChosenMuxPath == 0 ? 8 : 12;
            var down = EvaluateMux(downMuxVisualizer.CurrentChosenMuxPath, left, -8, -12);

            busController.StartBusSignal(buses.outputMuxToRegister, _output.Input, true);
            yield return WaitNoSignals;

            busController.StartBusSignal(buses.upperMuxToOutputMux, up, true);
            busController.StartBusSignal(buses.downMuxToOutputMux, down, true);
            busController.StartBusSignal(buses.constGToOutputMux, 4, true);
            yield return WaitNoSignals;

            busController.StartBusSignal(buses.middleMuxToDownMux, left, true);
            busController.StartBusSignal(buses.constCToDownMux, -8, true);
            busController.StartBusSignal(buses.constDToDownMux, -12, true);

            busController.StartBusSignal(buses.constEToUpperMux, -4, true);
            busController.StartBusSignal(buses.constFToUpperMux, 0, true);
            yield return WaitNoSignals;

            busController.StartBusSignal(buses.constAToMiddleMux, 8, true);
            busController.StartBusSignal(buses.constBToMiddleMux, 12, true);

            _currentBus--;
        }

        yield return WaitNoSignals;
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var up = EvaluateMux(upperMuxVisualizer.CurrentChosenMuxPath, -4, 0, -1);
            var left = EvaluateMux(middleMuxVisualizer.CurrentChosenMuxPath, 8, 12, -1);
            var down = EvaluateMux(downMuxVisualizer.CurrentChosenMuxPath, left, -8, -12);
            var output = EvaluateMux(outputMuxVisualizer.CurrentChosenMuxPath, up, 4, down);

            busController.StartBusSignal(buses.constAToMiddleMux, 8);
            busController.StartBusSignal(buses.constBToMiddleMux, 12);

            yield return WaitNoSignals;

            busController.StartBusSignal(buses.middleMuxToDownMux, left);
            busController.StartBusSignal(buses.constCToDownMux, -8);
            busController.StartBusSignal(buses.constDToDownMux, -12);

            busController.StartBusSignal(buses.constEToUpperMux, -4);
            busController.StartBusSignal(buses.constFToUpperMux, 0);

            yield return WaitNoSignals;

            busController.StartBusSignal(buses.upperMuxToOutputMux, up);
            busController.StartBusSignal(buses.downMuxToOutputMux, down);
            busController.StartBusSignal(buses.constGToOutputMux, 4);

            yield return WaitNoSignals;

            busController.StartBusSignal(buses.outputMuxToRegister, output);

            _currentBus++;
        }

        yield return WaitNoSignals;
    }

    protected override void UpdateVisualizers()
    {
        _infoOutputRegister.Display("Register 1", _output.Output);
    }
}