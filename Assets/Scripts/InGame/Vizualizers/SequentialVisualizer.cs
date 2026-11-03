using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/*
 Base class for visualizers of clocked components (registers, register file, memories).
 Shared behaviour: the WE toggle (button in the panel + STOP indicator on the model)
 and blinking on the clock edge.
*/
public abstract class SequentialVisualizer : BaseVisualizer
{
    [FormerlySerializedAs("_writeEnableIndicator")]
    [Header("Write Enable Visualization")]
    [Tooltip("Object that shows the STOP image while WE is off. Without it the player cannot toggle WE.")]
    [SerializeField] private GameObject writeEnableIndicator;
    public bool isWriteEnabled;

    [FormerlySerializedAs("_blinker")]
    [Header("Blinker of sequential component")]
    [SerializeField] private Blinker blinker;

    private Button _weButton;

    protected override void Awake()
    {
        base.Awake();

        if (panelInstance.TryGetComponent<IWriteEnablePanel>(out var wePanel))
            _weButton = wePanel.WeButton;

        // initial state of the STOP indicator
        if (writeEnableIndicator != null)
        {
            writeEnableIndicator.SetActive(false);
            isWriteEnabled = true;
            if (_weButton != null) _weButton.onClick.AddListener(SwitchWriteEnableVisualization);
        }
    }

    public void ForceUpdateWriteEnableVisualization(bool flag)
    {
        if (writeEnableIndicator == null) return;
        isWriteEnabled = flag;
        writeEnableIndicator.SetActive(!isWriteEnabled);
    }

    public void TriggerBlink()
    {
        if (blinker != null) blinker.Trigger();
    }

    public override void SetInteractable(bool value)
    {
        if (_weButton != null) _weButton.interactable = value;
    }

    private void SwitchWriteEnableVisualization()
    {
        if (writeEnableIndicator == null) return;
        // WE on -> indicator hidden, WE off -> indicator (STOP) shown
        isWriteEnabled = !isWriteEnabled;
        writeEnableIndicator.SetActive(!isWriteEnabled);
        HideData();
    }
}
