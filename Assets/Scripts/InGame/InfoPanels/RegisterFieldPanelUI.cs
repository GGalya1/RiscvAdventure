using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RegisterFieldPanelUI : MonoBehaviour, IWriteEnablePanel
{
    private const string LOG_PATTERN_PC = "r15 (pc):\n{0}";
    private const string LOG_PATTERN_REGULAR = "r{0}: {1}";

    [FormerlySerializedAs("_weButton")] [SerializeField]
    private Button weButton;

    public TextMeshProUGUI titleText;

    public TextMeshProUGUI[] registerText;
    public Button WeButton => weButton;

    private void Awake()
    {
        titleText.text = "Register Field";
    }

    public void Display(IReadOnlyList<int> values)
    {
        for (var i = 0; i < registerText.Length && i < values.Count; i++)
        {
            if (i == 15)
                registerText[i].SetText(LOG_PATTERN_PC, values[i]);
            else
                registerText[i].SetText(LOG_PATTERN_REGULAR, i, values[i]);
        }
    }
}