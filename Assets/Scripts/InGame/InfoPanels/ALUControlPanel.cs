using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

//  Panel with four operation buttons; used by the ALU and the extend unit
public class AluControlPanel : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    [FormerlySerializedAs("_firstOperationButton")] [SerializeField] private Button firstOperationButton;
    [FormerlySerializedAs("_secondOperationButton")] [SerializeField] private Button secondOperationButton;
    [FormerlySerializedAs("_thirdOperationButton")] [SerializeField] private Button thirdOperationButton;
    [FormerlySerializedAs("_fourthOperationButton")] [SerializeField] private Button fourthOperationButton;

    private Button[] _operationButtons;

    // The buttons in operation order (index = value of the control signal)
    public Button[] OperationButtons => _operationButtons ??= new[]
    {
        firstOperationButton, secondOperationButton, thirdOperationButton, fourthOperationButton
    };

    public void Setup(string title)
    {
        titleText.text = title;
    }
}