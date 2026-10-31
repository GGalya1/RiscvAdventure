using UnityEngine;
using UnityEngine.Serialization;

public class MultiplexerVisualizer: BaseVisualizer
{
    [FormerlySerializedAs("_inputBuses")]
    [Header("Bus Line Renderers")]
    [SerializeField] private LineRenderer[] inputBuses;

    [FormerlySerializedAs("_outputBus")] [SerializeField] private LineRenderer outputBus;
    [FormerlySerializedAs("_controlBus")] [SerializeField] private LineRenderer controlBus;

    [FormerlySerializedAs("_bitRenderers")]
    [Header("Visual Bits Renderers")]
    [SerializeField] private Renderer[] bitRenderers;

    [FormerlySerializedAs("_disabledColor")]
    [Header("Colors")]
    [SerializeField] private Color disabledColor = Color.gray;
    [FormerlySerializedAs("_activeColor")] [SerializeField] private Color activeColor = Color.red;

    public MultiplexerControlPanel UIController { get; private set; }

    public int CurrentChosenMuxPath { get; private set; } = -1;


    protected override void Awake()
    {
        base.Awake();

 
        if (UIController != null)
        {
            if (bitRenderers.Length > 2) 
            {
                UIController.Setup(true, true, true, "Multiplexer 3");
            }
            else
            {
                UIController.Setup(true, true, false, "Multiplexer 2");
            }
        }

        UIController.FirstWayButton.onClick.AddListener(() => SelectPath(0));
        UIController.SecondWayButton.onClick.AddListener(() => SelectPath(1));
        UIController.ThirdWayButton.onClick.AddListener(() => SelectPath(2));

        ResetVisualisation();
    }
    protected override void InitializePanelController()
    {
        // Controller initialization specific to this class
        UIController = panelInstance.GetComponent<MultiplexerControlPanel>();
        if (UIController == null)
        {
            CustomLog.LogEditorError($"MultiplexerControlPanel component not found on the prefab for {gameObject.name}!");
        }
    }

    public override void ResetVisualisation() {
        CurrentChosenMuxPath = -1;
        UpdateVisuals(-1);
    }
    public void SelectPath(int index)
    {
        if (index < 0 || index >= inputBuses.Length) return;

        // Debug.Log($"Path {index + 1} chosen");
        CurrentChosenMuxPath = index;
        UpdateVisuals(index);
        HideData();
    }

    #region helpers
    private void UpdateVisuals(int activeIndex)
    {
        for (var i = 0; i < inputBuses.Length; i++)
        {
            var isActive = (i == activeIndex);
            RendererColor.Set(inputBuses[i], isActive ? activeColor : disabledColor);

            if (i < bitRenderers.Length)
                RendererColor.Set(bitRenderers[i], isActive ? activeColor : disabledColor);
        }

        RendererColor.Set(outputBus, activeIndex != -1 ? activeColor : disabledColor);
    }

    public override void SetInteractable(bool value) {
        UIController.FirstWayButton.interactable = value;
        UIController.SecondWayButton.interactable = value;

        if (UIController.ThirdWayButton != null)
            UIController.ThirdWayButton.interactable = value;
    }
    #endregion
}
