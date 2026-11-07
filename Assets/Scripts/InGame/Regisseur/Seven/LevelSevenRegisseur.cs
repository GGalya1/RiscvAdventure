using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelSevenState
{
    public RegisterState SrcA;
    public RegisterState SrcB;
    public RegisterState Output;
    public RegisterFileState RegisterFile;
    public AluOperation AluOperation;
}

[Serializable]
public class LevelSevenBusSegments
{
    [Tooltip("SrcA register (rs1 address) -> Register File A1")]
    public LineRenderer srcAToRegFileA1;

    [Tooltip("SrcB register (rs2 address) -> Register File A2")]
    public LineRenderer srcBToRegFileA2;

    [Tooltip("Register File RD1 -> ALU input A")]
    public LineRenderer rd1ToAlu;

    [Tooltip("Register File RD2 -> ALU input B")]
    public LineRenderer rd2ToAlu;

    [Tooltip("ALU result -> Output register")]
    public LineRenderer aluToOutput;
}

public class LevelSevenRegisseur : BaseLevelRegisseur<LevelSevenState, LevelSevenBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")] [Header("Level 7 Specific Components")] [SerializeField]
    protected RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerSrcBVisualizer")] [SerializeField]
    protected RegisterVisualizer registerSrcBVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    protected RegisterVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("_registerFileVisualizer")] [SerializeField]
    protected RegisterFileVisualizer registerFileVisualizer;

    [FormerlySerializedAs("_aluVizualizer")] [SerializeField]
    protected AluVisualiser aluVisualizer;

    private int _currentBus; // [0, 2]
    private Register _output;
    private RegisterFile _registerFile;

    // Intern components for computations
    private Register _srcA;
    private Register _srcB;

    protected override int RightAnswerValue => 42;

    private readonly Alu _alu = new();

    protected override void OnLevelStart()
    {
        aluVisualizer.Bind(_alu);

        // Initialization of logical components
        _srcA = new Register(6);
        _srcB = new Register(7);
        _output = new Register();

        _registerFile = new RegisterFile(new[]
        {
            0, 1, 39, 43, 5, 6, 2,
            40, 1, 39, 13, 56, 64, 20,
            50, 51, 0, 12, 53, 65, 29,
            60, 61, 0, 1, 54, 0, 28,
            70, 30, 31, 0
        });

        Clock.Add(_srcA, _srcB, _output, _registerFile);

        Show(
            registerSrcAVisualizer.Bind(_srcA, "Register 1"),
            registerSrcBVisualizer.Bind(_srcB, "Register 2"),
            registerOutputVisualizer.Bind(_output, "Register 3"),
            registerFileVisualizer.Bind(_registerFile));
    }

    protected override void ApplyState(LevelSevenState s)
    {
        _srcA.Restore(s.SrcA);
        _srcB.Restore(s.SrcB);
        _output.Restore(s.Output);
        _registerFile.Restore(s.RegisterFile);
        _alu.Operation = s.AluOperation;
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override LevelSevenState GetCurrentState() => new()
    {
        SrcA = _srcA.Save(),
        SrcB = _srcB.Save(),
        Output = _output.Save(),
        RegisterFile = _registerFile.Save(),
        AluOperation = _alu.Operation
    };

    protected override void HandleClockUpdate()
    {
        // implementation
        _registerFile.ReadAddress1 = _srcA.Output;
        _registerFile.ReadAddress2 = _srcB.Output;
        _output.Input = _alu.Compute(_registerFile.ReadData1, _registerFile.ReadData2);

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        if (_currentBus >= 1 && _currentBus <= maxTickNumber)
        {
            var before = StateAt(TickCounter);

            yield return Bus.WaveBack((buses.aluToOutput, _output.Input));
            yield return Bus.WaveBack(
                (buses.rd1ToAlu, _registerFile.Registers[before.SrcA.Value]),
                (buses.rd2ToAlu, _registerFile.Registers[before.SrcB.Value]));
            yield return Bus.WaveBack((buses.srcAToRegFileA1, before.SrcA.Value), (buses.srcBToRegFileA2, before.SrcB.Value));

            _currentBus--;
        }
    }

    protected override IEnumerator RunBusVisualizations()
    {
        if (_currentBus >= 0 && _currentBus < maxTickNumber)
        {
            var rd1 = _registerFile.ReadData1;
            var rd2 = _registerFile.ReadData2;

            yield return Bus.Wave((buses.srcAToRegFileA1, _srcA.Output), (buses.srcBToRegFileA2, _srcB.Output));
            yield return Bus.Wave((buses.rd1ToAlu, rd1), (buses.rd2ToAlu, rd2));
            yield return Bus.Wave((buses.aluToOutput, _alu.Compute(rd1, rd2)));

            _currentBus++;
        }
    }
}