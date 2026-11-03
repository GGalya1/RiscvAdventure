using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class InstrMemoryControlPanel : InfoPanelUI, IWriteEnablePanel
{
    [FormerlySerializedAs("_weButton")] [SerializeField] private Button weButton;
    public Button WeButton => weButton;

    [FormerlySerializedAs("firstAddresValue")] public TextMeshProUGUI firstAddressValue;
    [FormerlySerializedAs("secondAddresValue")] public TextMeshProUGUI secondAddressValue;
    [FormerlySerializedAs("thirdAddresValue")] public TextMeshProUGUI thirdAddressValue;
    [FormerlySerializedAs("fourthAddresValue")] public TextMeshProUGUI fourthAddressValue;

    public void Display(int firstVal, int secondVal, int thirdVal, int fourthVal)
    {
        firstAddressValue.SetText("{0}", firstVal);
        secondAddressValue.SetText("{0}", secondVal);
        thirdAddressValue.SetText("{0}", thirdVal);
        fourthAddressValue.SetText("{0}", fourthVal);
    }

    public void Display(string firstVal, string secondVal, string thirdVal, string fourthVal)
    {
        firstAddressValue.text = firstVal;
        secondAddressValue.text = secondVal;
        thirdAddressValue.text = thirdVal;
        fourthAddressValue.text = fourthVal;
    }
}