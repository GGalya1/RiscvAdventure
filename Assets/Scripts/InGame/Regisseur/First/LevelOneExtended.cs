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

    private Register _output;

    protected override int RightAnswerValue => 12;

    private readonly Multiplexer _upperMux = new();
    private readonly Multiplexer _middleMux = new();
    private readonly Multiplexer _downMux = new();
    private readonly Multiplexer _outputMux = new();

    protected override void OnLevelStart()
    {
        upperMuxVisualizer.Bind(_upperMux);
        middleMuxVisualizer.Bind(_middleMux);
        downMuxVisualizer.Bind(_downMux);
        outputMuxVisualizer.Bind(_outputMux);

        _output = new Register(11);
        Clock.Add(_output);

        Show(registerOutputVisualizer.Bind(_output, "Register 1"));
    }

    protected override void ApplyState(ExtendedFirstLevelState s) // "s" for state
    {
        _output.Reset(s.RegisterOutputValue);

        _upperMux.SelectedInput = s.MuXup;
        _middleMux.SelectedInput = s.MuXmiddle;
        _downMux.SelectedInput = s.MuXdown;
        _outputMux.SelectedInput = s.MuXoutput;
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

            MuXup = _upperMux.SelectedInput,
            MuXmiddle = _middleMux.SelectedInput,
            MuXdown = _downMux.SelectedInput,
            MuXoutput = _outputMux.SelectedInput
        };
    }

    protected override void HandleClockUpdate()
    {
        var up = _upperMux.Route(-4, 0, -1);
        var left = _middleMux.Route(8, 12, -1);
        var down = _downMux.Route(left, -8, -12);

        _output.Input = _outputMux.Route(up, 4, down);

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            var up = _upperMux.SelectedInput == 0 ? -4 : 0;
            var left = _middleMux.SelectedInput == 0 ? 8 : 12;
            var down = _downMux.Route(left, -8, -12);

            yield return Bus.WaveBack((buses.outputMuxToRegister, _output.Input));
            yield return Bus.WaveBack((buses.upperMuxToOutputMux, up), (buses.downMuxToOutputMux, down), (buses.constGToOutputMux, 4));
            yield return Bus.WaveBack(
                (buses.middleMuxToDownMux, left), (buses.constCToDownMux, -8), (buses.constDToDownMux, -12),
                (buses.constEToUpperMux, -4), (buses.constFToUpperMux, 0));
            yield return Bus.WaveBack((buses.constAToMiddleMux, 8), (buses.constBToMiddleMux, 12));

            _currentBus--;
        }
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var up = _upperMux.Route(-4, 0, -1);
            var left = _middleMux.Route(8, 12, -1);
            var down = _downMux.Route(left, -8, -12);
            var output = _outputMux.Route(up, 4, down);

            yield return Bus.Wave((buses.constAToMiddleMux, 8), (buses.constBToMiddleMux, 12));
            yield return Bus.Wave(
                (buses.middleMuxToDownMux, left), (buses.constCToDownMux, -8), (buses.constDToDownMux, -12),
                (buses.constEToUpperMux, -4), (buses.constFToUpperMux, 0));
            yield return Bus.Wave((buses.upperMuxToOutputMux, up), (buses.downMuxToOutputMux, down), (buses.constGToOutputMux, 4));
            yield return Bus.Wave((buses.outputMuxToRegister, output));

            _currentBus++;
        }
    }
}