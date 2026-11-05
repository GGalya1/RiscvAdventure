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

        var buttons = UIController.WayButtons;
        for (var i = 0; i < buttons.Length; i++)
        {
            var path = i;
            buttons[i].onClick.AddListener(() => SelectPath(path));
        }

        ResetVisualisation();
    }
    protected override void InitializePanel()
    {
        UIController = RequirePanel<MultiplexerControlPanel>();
    }

    public void ResetVisualisation() {
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
        foreach (var button in UIController.WayButtons)
        {
            if (button != null) button.interactable = value;
        }
    }
    #endregion
}
