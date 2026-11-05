using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelFiveExtendedState
{
    public int RegisterValue;
    public int RegisterOutputValue;

    public bool RegisterWe;
    public bool RegisterOutputWe;

    public int ExtenderOperation;
}

[Serializable]
public class LevelFiveExtendedBusSegments: IBusSegmentProvider
{
    [Tooltip("SrcA register (raw instruction word) -> Extend unit")]
    public LineRenderer srcAToExtend;

    [Tooltip("Extend unit output -> Output register")]
    public LineRenderer extendToOutput;

    public void RegisterAll(BusController c)
    {
        c.RegisterSegment(srcAToExtend);
        c.RegisterSegment(extendToOutput);
    }
}

public class LevelFiveExtended : BaseLevelRegisseur<LevelFiveExtendedState, LevelFiveExtendedBusSegments>
{
    [FormerlySerializedAs("_registerSrcAVisualizer")]
    [Header("Level 5 (Extended) Specific Components")]
    [SerializeField]
    private RegisterVisualizer registerSrcAVisualizer;

    [FormerlySerializedAs("_registerOutputVisualizer")] [SerializeField]
    private RegisterVisualizer registerOutputVisualizer;

    [FormerlySerializedAs("extenderVizualizer")] [FormerlySerializedAs("_extenderVizualizer")] [SerializeField]
    private ExtenderVisualizer extenderVisualizer;

    [FormerlySerializedAs("InputRegisterValue")] [SerializeField]
    private uint inputRegisterValue;
    

    private Register _output;

    private Register _srcA;

    protected int CurrentBus = 0;

    protected override void OnLevelStart()
    {
        _srcA = new Register((int)inputRegisterValue);
        _output = new Register();

        Clock.Add(_srcA, _output);

        _infoSrcARegister = registerSrcAVisualizer.UIRegisterPanel;
        _infoOutputRegister = registerOutputVisualizer.UIRegisterPanel;

        UpdateVisualizers();
    }

    protected override void ApplyState(LevelFiveExtendedState s)
    {
        _srcA.Reset(s.RegisterValue);
        _output.Reset(s.RegisterOutputValue);

        _srcA.WriteEnable = s.RegisterWe;
        _output.WriteEnable = s.RegisterOutputWe;

        extenderVisualizer.ChooseAluOperation(s.ExtenderOperation);
    }

    protected override void BlinkClockedComponents()
    {
        registerSrcAVisualizer.TriggerBlink();
        registerOutputVisualizer.TriggerBlink();
    }

    protected override bool CheckWinCondition()
    {
        return _output.Output == RightAnswerValue;
    }

    protected override LevelFiveExtendedState GetCurrentState()
    {
        return new LevelFiveExtendedState
        {
            RegisterValue = _srcA.Output,
            RegisterOutputValue = _output.Output,

            RegisterWe = _srcA.WriteEnable,
            RegisterOutputWe = _output.WriteEnable,

            ExtenderOperation = extenderVisualizer.CurrentAluOperation
        };
    }

    protected override void HandleClockUpdate()
    {
        // synchronize visualizers and concrete objects
        _srcA.WriteEnable = registerSrcAVisualizer.isWriteEnabled;
        _output.WriteEnable = registerOutputVisualizer.isWriteEnabled;

        _output.Input = Extender.Evaluate((ImmediateType)extenderVisualizer.CurrentAluOperation, (uint)_srcA.Output);

        Clock.Tick();
    }

    protected override IEnumerator ReverseBusVisualizations()
    {
        yield return Bus.WaveBack((buses.srcAToExtend, _srcA.Output), (buses.extendToOutput, _output.Input));
    }

    protected override IEnumerator RunBusVisualizations()
    {
        yield return Bus.Wave((buses.srcAToExtend, _srcA.Output), (buses.extendToOutput, _output.Output));
    }

    protected override void UpdateVisualizers()
    {
        _infoSrcARegister.Display("Register 1", RiscVDecoder.CommandBuilder((uint)_srcA.Output));
        _infoOutputRegister.Display("Register 2", _output.Output);
        registerSrcAVisualizer.ForceUpdateWriteEnableVisualization(_srcA.WriteEnable);
        registerOutputVisualizer.ForceUpdateWriteEnableVisualization(_output.WriteEnable);
    }

    #region CACHED UI REFERENCES

    private InfoPanelUI _infoSrcARegister;
    private InfoPanelUI _infoOutputRegister;

    #endregion
}