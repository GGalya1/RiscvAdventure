using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// View of a <see cref="Multiplexer"/>: the way buttons set the selected input,
/// the selected input bus, its bit indicator and the output bus are highlighted.
/// </summary>
public class MultiplexerVisualizer : BaseVisualizer
{
    [FormerlySerializedAs("_inputBuses")]
    [Header("Bus Line Renderers")]
    [SerializeField] private LineRenderer[] inputBuses;

    [FormerlySerializedAs("_outputBus")] [SerializeField] private LineRenderer outputBus;
    [FormerlySerializedAs("_controlBus")] [SerializeField] private LineRenderer controlBus; // currently not highlighted

    [FormerlySerializedAs("_bitRenderers")]
    [Header("Visual Bits Renderers")]
    [SerializeField] private Renderer[] bitRenderers;

    [FormerlySerializedAs("_disabledColor")]
    [Header("Colors")]
    [SerializeField] private Color disabledColor = Color.gray;
    [FormerlySerializedAs("_activeColor")] [SerializeField] private Color activeColor = Color.red;

    private MultiplexerControlPanel _panel;
    private Multiplexer _mux;

    protected override void InitializePanel()
    {
        _panel = RequirePanel<MultiplexerControlPanel>();
    }

    protected override void Awake()
    {
        base.Awake();

        if (_panel != null)
        {
            var hasThirdInput = bitRenderers.Length > 2;
            _panel.Setup(true, true, hasThirdInput, hasThirdInput ? "Multiplexer 3" : "Multiplexer 2");

            var buttons = _panel.WayButtons;
            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => OnWayButton(index));
            }
        }

        UpdateVisuals(Multiplexer.NoInput);
    }

    public MultiplexerVisualizer Bind(Multiplexer mux)
    {
        Unbind();
        _mux = mux;
        _mux.SelectionChanged += OnSelectionChanged;
        UpdateVisuals(_mux.SelectedInput);
        return this;
    }

    private void OnWayButton(int index)
    {
        if (_mux == null || !IsValidInput(index)) return;
        _mux.SelectedInput = index;
    }

    private void OnSelectionChanged(int selectedInput)
    {
        if (selectedInput == Multiplexer.NoInput)
        {
            UpdateVisuals(Multiplexer.NoInput);
            return;
        }

        if (!IsValidInput(selectedInput)) return;

        UpdateVisuals(selectedInput);
        HideData();
    }

    private bool IsValidInput(int index) => index >= 0 && index < inputBuses.Length;

    private void UpdateVisuals(int activeIndex)
    {
        for (var i = 0; i < inputBuses.Length; i++)
        {
            var color = i == activeIndex ? activeColor : disabledColor;
            RendererColor.Set(inputBuses[i], color);

            if (i < bitRenderers.Length)
                RendererColor.Set(bitRenderers[i], color);
        }

        RendererColor.Set(outputBus, activeIndex != Multiplexer.NoInput ? activeColor : disabledColor);
    }

    public override void SetInteractable(bool value)
    {
        if (_panel == null) return;

        foreach (var button in _panel.WayButtons)
        {
            if (button != null) button.interactable = value;
        }
    }

    private void Unbind()
    {
        if (_mux != null) _mux.SelectionChanged -= OnSelectionChanged;
        _mux = null;
    }

    private void OnDestroy() => Unbind();
}
