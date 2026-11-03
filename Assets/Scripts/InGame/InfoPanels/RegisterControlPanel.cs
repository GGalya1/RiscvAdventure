using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RegisterControlPanel : InfoPanelUI, IWriteEnablePanel
{
    [FormerlySerializedAs("_weButton")] [SerializeField] private Button weButton;
    public Button WeButton => weButton;
}