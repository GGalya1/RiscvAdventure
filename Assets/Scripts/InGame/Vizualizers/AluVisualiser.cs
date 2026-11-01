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

        uiController.FirstOperationButton.onClick.AddListener(() => ChooseAluOperation(0));
        uiController.SecondOperationButton.onClick.AddListener(() => ChooseAluOperation(1));
        uiController.ThirdOperationButton.onClick.AddListener(() => ChooseAluOperation(2));
        uiController.FourthOperationButton.onClick.AddListener(() => ChooseAluOperation(3));

        ResetVisualisation();
    }
    protected override void InitializePanel()
    {
        uiController = RequirePanel<AluControlPanel>();
    }

    public override void SetInteractable(bool value)
    {
        uiController.FirstOperationButton.interactable = value;
        uiController.SecondOperationButton.interactable = value;
        uiController.ThirdOperationButton.interactable = value;
        uiController.FourthOperationButton.interactable = value;
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
