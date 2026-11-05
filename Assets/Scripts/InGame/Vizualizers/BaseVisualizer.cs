using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base class of every clickable processor component in the scene.
/// Handles "click to open the info panel": panel animation and highlighting the 3D model.
/// </summary>
public abstract class BaseVisualizer : MonoBehaviour, IVisualizer
{
    [FormerlySerializedAs("_panelInstance")] [SerializeField] protected GameObject panelInstance;
    [FormerlySerializedAs("_panelCanvasGroup")] [SerializeField] private CanvasGroup panelCanvasGroup;

    [FormerlySerializedAs("_selectionColor")]
    [Header("Visual Settings")]
    [SerializeField] protected Color selectionColor = Color.blue;

    [FormerlySerializedAs("_animDuration")]
    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.25f;
    [FormerlySerializedAs("_moveDistance")] [SerializeField] private float moveDistance = 0.2f;

    // Renderer that is tinted with selectionColor while the panel is open (optional)
    [FormerlySerializedAs("_bigModelRenderer")] [SerializeField] protected Renderer bigModelRenderer;

    private PanelAnimator _panelAnimator;
    private Color _originalColor;
    private bool _isPanelOpen;

    protected virtual void Awake()
    {
        _originalColor = RendererColor.Original(bigModelRenderer);

        _panelAnimator = new PanelAnimator(panelInstance.transform, panelCanvasGroup, animDuration, moveDistance);
        _panelAnimator.HideImmediately();

        InitializePanel();
    }

    // Called from Awake: fetch the panel component this visualizer works with
    protected abstract void InitializePanel();

    #region IVisualizer
    public void ShowData()
    {
        if (_isPanelOpen)
        {
            HideData();
            return;
        }

        _isPanelOpen = true;
        RendererColor.Set(bigModelRenderer, selectionColor);
        _panelAnimator.Show();
    }

    public void HideData()
    {
        _isPanelOpen = false;
        RendererColor.Set(bigModelRenderer, _originalColor);
        _panelAnimator.Hide();
    }
    #endregion

    /// <summary> Enables/disables the panel buttons (the level locks them while a tick is animated). </summary>
    public virtual void SetInteractable(bool value) { }

    // Puts the panel into the hidden state without animation
    protected void HidePanelImmediately()
    {
        _panelAnimator.HideImmediately();
    }

    // Returns the panel component of generic type T and logs an error if it is missing
    protected T RequirePanel<T>() where T : Component
    {
        if (panelInstance.TryGetComponent<T>(out var panel)) return panel;

        CustomLog.LogEditorError($"{typeof(T).Name} component not found on the panel of {gameObject.name}!");
        return null;
    }
}
