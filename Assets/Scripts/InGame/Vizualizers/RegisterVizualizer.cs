using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Component for visualizing a digital Register unit within a 3D scene.
/// It is responsible for: displaying the data UI panel, changing the model's color 
/// upon activation, and flashing (blinking) when a Clock signal is triggered.
/// It inherits common visualization logic from <see cref="BaseVisualizer"/>.
/// </summary>
public class RegisterVisualizer: BaseVisualizer
{
    /// <summary>
    /// The cached UI controller for the information panel (InfoPanelUI), 
    /// instantiated during Awake. Provides access for the Level_X_Regisseur 
    /// to update the displayed register value.
    /// </summary>
    public RegisterControlPanel UIRegisterPanel { get; private set; }

    [FormerlySerializedAs("_writeEnableIndicator")]
    [Header("Write Enable Visualization")]
    [Tooltip("Object that controls WE-signal and Stop-image")]
    [SerializeField] private GameObject writeEnableIndicator;
    public bool isWriteEnabled;

    [FormerlySerializedAs("_blinker")]
    [Header("Blinker of sequential component")]
    [SerializeField] private Blinker blinker;

    protected override void Awake() {
        base.Awake();

        // Set the initial/default data on the UI panel
        if (UIRegisterPanel != null)
        {
            UIRegisterPanel.Display("N/A", 0);
        }

        // Set the initial state for STOP indicator
        if (writeEnableIndicator != null)
        {
            writeEnableIndicator.SetActive(false);
            isWriteEnabled = true;
            UIRegisterPanel.WeButton.onClick.AddListener(SwitchWriteEnableVisualization);
        }
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
    
    public override void SetInteractable(bool value)
    {
        UIRegisterPanel.WeButton.interactable = value;
    }
    
    public void ForceUpdateWriteEnableVisualization(bool flag)
    {
        if (writeEnableIndicator == null) return;
        isWriteEnabled = flag;
        writeEnableIndicator.SetActive(!isWriteEnabled);
    }

    protected override void InitializePanel()
    {
        UIRegisterPanel = RequirePanel<RegisterControlPanel>();
    }

    public void TriggerBlink()
    {
        blinker.Trigger();
    }

    public override void ResetVisualisation() { }
}
