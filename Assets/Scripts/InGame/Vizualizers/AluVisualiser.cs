using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class AluVisualiser : BaseVisualizer
{
    [FormerlySerializedAs("_operationBanner")]
    [Header("Operations Renderers (Optional 3D Text/Objects)")]
    [SerializeField] protected GameObject operationBanner;
    [FormerlySerializedAs("_symbolForOperation")] [SerializeField] protected TMP_Text symbolForOperation;

    [FormerlySerializedAs("_uiController")] public AluControlPanel uiController;

    protected int Operation;
    public int CurrentAluOperation => Operation;


    protected override void Awake()
    {
        base.Awake();

        if (uiController != null)
        {
            uiController.Setup("ALU");
        }

        var buttons = uiController.OperationButtons;
        for (var i = 0; i < buttons.Length; i++)
        {
            var operation = i;
            buttons[i].onClick.AddListener(() => ChooseAluOperation(operation));
        }

        ResetVisualisation();
    }
    protected override void InitializePanel()
    {
        uiController = RequirePanel<AluControlPanel>();
    }

    public override void SetInteractable(bool value)
    {
        foreach (var button in uiController.OperationButtons) button.interactable = value;
    }

    public override void ResetVisualisation(){
        if (operationBanner.activeSelf) {
            operationBanner.SetActive(false);
        }
    }

    public void ChooseAluOperation(int operation)
    {
        HideData();
        var symbol = operation switch
        {
            0 => "+" // ADD
            ,
            1 => "-" // SUBTRACT
            ,
            2 => "&" // MULTIPLY (logic AND)
            ,
            3 => "|" // DIVIDE (logic OR)
            ,
            _ => "?"
        };

        symbolForOperation.text = symbol;
        Operation = operation;

        if (!operationBanner.activeSelf) {
            operationBanner.SetActive(true);
        }
    }
}