using UnityEngine;

public class ExtenderVisualizer : AluVisualiser
{
    protected override void Awake()
    {
        base.Awake();

        if (uiController != null)
        {
            uiController.Setup("Extender");
        }

        var buttons = uiController.OperationButtons;
        for (var i = 0; i < buttons.Length; i++)
        {
            var operationType = i;
            buttons[i].onClick.AddListener(() => ChooseExtenderType(operationType));
        }

        ResetVisualisation();
    }
    protected override void InitializePanel()
    {
        uiController = RequirePanel<AluControlPanel>();
    }

    private void ChooseExtenderType(int operationType)
    {
        string symbol;
        switch (operationType)
        {
            case 0:
                symbol = "I"; // I-Type (lw=> immediate)
                break;
            case 1:
                symbol = "S"; // S-Type (store)
                break;
            case 2:
                symbol = "B"; // B-Type (branch)
                break;
            case 3:
                symbol = "J"; // J-Type (jump)
                break;
            default:
                CustomLog.LogEditorWarning($"Extender operation is not valid and is equal {operationType}. Displaying '?'");
                symbol = "?";
                break;
        }

        symbolForOperation.text = symbol;
        Operation = operationType;

        if (!operationBanner.activeSelf)
        {
            operationBanner.SetActive(true);
        }
    }
}