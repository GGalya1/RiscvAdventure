using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class MultiplexerControlPanel: MonoBehaviour
{
    public TextMeshProUGUI titleText;
    [FormerlySerializedAs("_firstWayButton")] [SerializeField] private Button firstWayButton;
    [FormerlySerializedAs("_secondWayButton")] [SerializeField] private Button secondWayButton;
    [FormerlySerializedAs("_thirdWayButton")] [SerializeField] private Button thirdWayButton;

    private Button[] _wayButtons;

    // The buttons in input order (index = selected multiplexer input). Entries may be null
    public Button[] WayButtons => _wayButtons ??= new[] { firstWayButton, secondWayButton, thirdWayButton };

    public void Setup(bool firstButton, bool secondButton, bool thirdButton, string title)
    {
        if (firstWayButton != null) firstWayButton.gameObject.SetActive(firstButton);
        if (secondWayButton != null) secondWayButton.gameObject.SetActive(secondButton);
        if (thirdWayButton != null) thirdWayButton.gameObject.SetActive(thirdButton);

        titleText.text = title;
    }
}