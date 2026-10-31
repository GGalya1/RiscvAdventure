using UnityEngine;
using UnityEngine.Serialization;

public class RegisterFileVisualizer : BaseVisualizer
{
    public RegisterFieldPanelUI UIRegisterPanel { get; private set; }

    [FormerlySerializedAs("_writeEnableIndicator")]
    [Header("Write Enable Visualization")]
    [Tooltip("Object that controls WE-signal and Stop-image")]
    [SerializeField] private GameObject writeEnableIndicator;
    public bool isWriteEnabled;

    [FormerlySerializedAs("_blinker")]
    [Header("Blinker of sequential component")]
    [SerializeField] private Blinker blinker;

    protected override void Awake()
    {
        base.Awake();

        // Set the initial/default data on the UI panel
        if (UIRegisterPanel != null)
        {
            UIRegisterPanel.Display(new int[16]);
        }

        // Set the initial state for STOP indicator
        if (writeEnableIndicator == null) return;
        writeEnableIndicator.SetActive(false);
        isWriteEnabled = true;
        UIRegisterPanel.WeButton.onClick.AddListener(SwitchWriteEnableVisualization);
    }

    public override void SetInteractable(bool value)
    {
        UIRegisterPanel.WeButton.interactable = value;
    }

    private void SwitchWriteEnableVisualization()
    {
        if (writeEnableIndicator == null) return;
        // if WriteEnable is true -> indicator must be inactive
        // if WriteEnable is false -> indicator must be active
        isWriteEnabled = !isWriteEnabled;
        writeEnableIndicator.SetActive(!isWriteEnabled);
        HideData();
    }
    public void ForceUpdateWriteEnableVisualization(bool flag)
    {
        if (writeEnableIndicator == null) return;
        isWriteEnabled = flag;
        writeEnableIndicator.SetActive(!isWriteEnabled);
    }

    protected override void InitializePanel()
    {
        UIRegisterPanel = RequirePanel<RegisterFieldPanelUI>();
    }

    public override void ResetVisualisation()
    {
        throw new System.NotImplementedException();
    }

    public void TriggerBlink()
    {
        blinker.Trigger();
    }
}
