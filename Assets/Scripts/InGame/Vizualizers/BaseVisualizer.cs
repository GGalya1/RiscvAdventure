using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

// An abstract base class for all component rendering
public abstract class BaseVisualizer : MonoBehaviour, IVisualizer
{
    // --- FIELDS FOR CONFIGURATION (Visible in Inspector) ---

    [FormerlySerializedAs("_panelInstance")] [SerializeField] protected GameObject panelInstance;
    [FormerlySerializedAs("_panelCanvasGroup")] [SerializeField] private CanvasGroup panelCanvasGroup;

    [FormerlySerializedAs("_selectionColor")]
    [Header("Visual Settings")]
    [SerializeField] protected Color selectionColor = Color.blue;

    [FormerlySerializedAs("_animDuration")]
    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.25f;
    [FormerlySerializedAs("_moveDistance")] [SerializeField] private float moveDistance = 0.2f;

    // Cached renderer, if you need to change the model's color (for a simple color change)
    [FormerlySerializedAs("_bigModelRenderer")] [SerializeField] protected Renderer bigModelRenderer;

    // A flag indicating whether the visualization is currently active
    private bool _isVisualizationActive;
    private Vector3 _targetLocalPos;

    // --- PUBLIC PROPERTIES (Read-Only access to configuration) ---

    public GameObject PanelPrefab => panelInstance;

    private Color _originalColor;

    // --- UNITY LIFECYCLE ---

    protected virtual void Awake()
    {
        _originalColor = RendererColor.Original(bigModelRenderer);

        // Creating and initializing the UI panel
        _targetLocalPos = panelInstance.transform.localPosition;
        PrepareHiddenState();

        // An abstract method that must be implemented by a subclass
        InitializePanelController();
    }

    // --- PUBLIC INTERFACE (IVisualizer) ---

    // ShowData and HideData share the same basic logic, but ShowData has some specific features
    public virtual void ShowData()
    {
        if (_isVisualizationActive)
        {
            HideData();
            return;
        }

        // General logic
        _isVisualizationActive = true;
        RendererColor.Set(bigModelRenderer, selectionColor);

        panelInstance.transform.DOKill();
        panelCanvasGroup.DOKill();

        panelInstance.transform.DOLocalMove(_targetLocalPos, animDuration).SetEase(Ease.OutCubic);
        panelCanvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutCubic).OnComplete(() => {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        });
    }

    public virtual void HideData()
    {
        _isVisualizationActive = false;
        RendererColor.Set(bigModelRenderer, _originalColor);

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        panelInstance.transform.DOKill();
        panelCanvasGroup.DOKill();

        panelCanvasGroup.DOFade(0f, animDuration).SetEase(Ease.InCubic);
        panelInstance.transform.DOLocalMove(_targetLocalPos - Vector3.up * moveDistance, animDuration)
                .SetEase(Ease.InCubic);  
    }

    // Abstract methods that must be implemented in subclasses
    public abstract void ResetVisualisation();
    protected abstract void InitializePanelController();

    public abstract void SetInteractable(bool value);
    
    // --- PRIVATE/PROTECTED METHODS ---
    protected void KillPanelTweens()
    {
        panelInstance.transform.DOKill();
        panelCanvasGroup.DOKill();
    }
    
    protected void PrepareHiddenState()
    {
        panelCanvasGroup.alpha = 0;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        panelInstance.transform.localPosition = _targetLocalPos - Vector3.up * moveDistance;
    }
}
