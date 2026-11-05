using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Base class for visualizers of clocked components (registers, register file, memories).
/// <br/>Shared behaviour: the WE toggle (button in the panel + STOP indicator on the model)
/// and blinking on the clock edge. The WE button writes directly into the bound model,
/// so the level never has to copy UI state into the simulation.
/// </summary>
public abstract class SequentialVisualizer : BaseVisualizer, IComponentView
{
    [FormerlySerializedAs("_writeEnableIndicator")]
    [Header("Write Enable Visualization")]
    [Tooltip("Object that shows the STOP image while WE is off. Without it the player cannot toggle WE.")]
    [SerializeField] private GameObject writeEnableIndicator;

    [FormerlySerializedAs("isWriteEnabled")]
    [Tooltip("WE value used when there is no indicator (the player cannot toggle WE then).")]
    [SerializeField] private bool fixedWriteEnable = true;

    [FormerlySerializedAs("_blinker")]
    [Header("Blinker of sequential component")]
    [SerializeField] private Blinker blinker;

    private Button _weButton;

    /// <summary> The bound simulation element. </summary>
    public ISequentialLogic Model { get; private set; }

    private bool CanToggleWriteEnable => writeEnableIndicator != null;

    protected override void Awake()
    {
        base.Awake();

        if (panelInstance.TryGetComponent<IWriteEnablePanel>(out var wePanel))
            _weButton = wePanel.WeButton;

        if (CanToggleWriteEnable)
        {
            writeEnableIndicator.SetActive(false);
            if (_weButton != null) _weButton.onClick.AddListener(ToggleWriteEnable);
        }
    }

    /// <summary> Connects this view to its simulation element. Call from the concrete Bind method. </summary>
    protected void BindModel(ISequentialLogic model)
    {
        Model = model;

        // Without the toggle UI the WE signal is a fixed scene setting.
        if (!CanToggleWriteEnable) model.WriteEnable = fixedWriteEnable;
    }

    public void Refresh()
    {
        if (Model == null) return;

        if (CanToggleWriteEnable) writeEnableIndicator.SetActive(!Model.WriteEnable);
        RefreshContent();
    }

    /// <summary> Writes the model state into the info panel. </summary>
    protected abstract void RefreshContent();

    public void Blink()
    {
        if (blinker != null) blinker.Trigger();
    }

    public override void SetInteractable(bool value)
    {
        if (_weButton != null) _weButton.interactable = value;
    }

    private void ToggleWriteEnable()
    {
        if (Model == null) return;

        Model.WriteEnable = !Model.WriteEnable;
        writeEnableIndicator.SetActive(!Model.WriteEnable);
        HideData();
    }
}
