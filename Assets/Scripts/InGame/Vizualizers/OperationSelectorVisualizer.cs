using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Base class for components with one control signal chosen from four buttons
/// (<see cref="AluVisualiser"/>, <see cref="ExtenderVisualizer"/>).
/// <br/>Button press -> subclass writes the control signal into its model ->
/// model raises its change event -> subclass calls <see cref="ShowOperation"/>.
/// The same path is taken when the level restores the model from history.
/// </summary>
public abstract class OperationSelectorVisualizer : BaseVisualizer
{
    [FormerlySerializedAs("_operationBanner")]
    [Header("Operations Renderers (Optional 3D Text/Objects)")]
    [SerializeField] private GameObject operationBanner;
    [FormerlySerializedAs("_symbolForOperation")] [SerializeField] private TMP_Text symbolForOperation;

    [FormerlySerializedAs("_uiController")] [SerializeField] private AluControlPanel uiController;

    // Title of the info panel
    protected abstract string Title { get; }

    // Called when the player presses the button with the given index
    protected abstract void OnOperationButton(int index);

    protected override void InitializePanel()
    {
        uiController = RequirePanel<AluControlPanel>();
    }

    protected override void Awake()
    {
        base.Awake();

        if (uiController != null)
        {
            uiController.Setup(Title);

            var buttons = uiController.OperationButtons;
            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => OnOperationButton(index));
            }
        }

        if (operationBanner.activeSelf) operationBanner.SetActive(false);
    }

    // Closes the panel and shows symbol on the 3D banner
    protected void ShowOperation(string symbol)
    {
        HideData();
        symbolForOperation.text = symbol;
        if (!operationBanner.activeSelf) operationBanner.SetActive(true);
    }

    public override void ResetVisualisation()
    {
        if (operationBanner.activeSelf) operationBanner.SetActive(false);
    }

    public override void SetInteractable(bool value)
    {
        if (uiController == null) return;

        foreach (var button in uiController.OperationButtons)
        {
            if (button != null) button.interactable = value;
        }
    }
}
