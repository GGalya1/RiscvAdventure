using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Plays the credits: fades in, auto-scrolls until the last line has left the screen, fades out.
///
/// - The scroll distance is calculated from the actual text height, so the credits can be
///   extended (or localized) without touching any values.
/// - The player can scroll manually (drag / touch / mouse wheel); auto-scroll resumes afterwards.
/// - The credits can be closed at any time with the close button or Escape / Android Back.
/// </summary>
public class CreditsScroller : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform textRect;
    [SerializeField] private CanvasGroup loadingOverlay;
    [SerializeField] private CanvasGroup creditsText;

    [Tooltip("Closes the credits (e.g. a ✕ in a corner). Must NOT be a child of Text Rect, otherwise it scrolls away. " +
             "It is only shown while the credits are open.")]
    [SerializeField] private Button closeButton;

    [Header("Viewport (optional) - leave EMPTY for full screen")]
    [Tooltip("Optional. Leave EMPTY (None) = credits scroll over the whole screen.\n" +
             "Only set this if the credits run inside a smaller panel/window: then drag that fixed panel " +
             "(usually the one with a Mask / RectMask2D) here.\n" +
             "Never use Text Rect or anything inside it - that's the moving text itself.")]
    [SerializeField] private RectTransform viewport;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 1f;

    [Tooltip("Auto-scroll speed in canvas units per second. The length of the credits doesn't matter anymore.")]
    [SerializeField, Min(1f)] private float scrollSpeed = 100f;

    [Tooltip("Extra distance after the last line has left the viewport.")]
    [SerializeField] private float endPadding = 50f;

    [Header("Manual Scrolling")]
    [SerializeField] private bool allowManualScroll = true;

    [Tooltip("Canvas units per mouse wheel notch.")]
    [SerializeField, Min(0f)] private float wheelStep = 120f;

    [Tooltip("Seconds after manual scrolling before the auto-scroll continues.")]
    [SerializeField, Min(0f)] private float resumeDelay = 1f;

    [Tooltip("Escape (and Android Back) closes the credits.")]
    [SerializeField] private bool closeOnEscape = true;

    [Header("Achievement")]
    [Tooltip("Also count the credits as watched if the player closes them early.")]
    [SerializeField] private bool countAsWatchedWhenClosed = true;

    private enum State { Idle, FadingIn, Playing, FadingOut }

    private State _state = State.Idle;
    private Vector2 _startPosition;
    private float _endY;
    private float _resumeTime;
    private Canvas _canvas;
    private Coroutine _routine;

    private bool _pointerDragging;
    private Pointer _dragPointer;
    private Vector2 _lastPointerLocal;

    private readonly Vector3[] _corners = new Vector3[4];

    public bool IsOpen => _state != State.Idle;

    private void Awake()
    {
        _startPosition = textRect.anchoredPosition;
        _canvas = textRect.GetComponentInParent<Canvas>();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseCredits);
            closeButton.gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Catch the common mistake: the moving text (or a child of it) used as the viewport.
        if (viewport != null && textRect != null && viewport.IsChildOf(textRect))
        {
            Debug.LogWarning("[CreditsScroller] Viewport must not be Text Rect or a child of it " +
                             "(that's the moving text). Cleared - empty means full screen.", this);
            viewport = null;
        }
    }
#endif

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseCredits);
    }

    public void LaunchCredits()
    {
        if (_state != State.Idle) return; // already running
        _routine = StartCoroutine(OpenRoutine());
    }

    /// <summary>Closes the credits early (close button, Escape, or from other scripts).</summary>
    public void CloseCredits()
    {
        if (_state == State.Idle || _state == State.FadingOut) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(CloseRoutine(false));
    }

    private void Update()
    {
        if (_state == State.Idle || _state == State.FadingOut) return;

        if (closeOnEscape && (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false))
        {
            CloseCredits();
            return;
        }

        if (_state == State.Playing) Tick();
    }

    #region Routines

    private IEnumerator OpenRoutine()
    {
        _state = State.FadingIn;

        textRect.anchoredPosition = _startPosition;
        _endY = CalculateEndY();
        _pointerDragging = false;
        _resumeTime = 0f;

        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (loadingOverlay != null)
        {
            loadingOverlay.blocksRaycasts = true;
            loadingOverlay.DOKill();
            loadingOverlay.DOFade(1f, transitionDuration).SetUpdate(true);
        }

        if (creditsText != null)
        {
            creditsText.DOKill();
            yield return creditsText.DOFade(1f, transitionDuration)
                .SetUpdate(true)
                .WaitForCompletion();
        }

        _state = State.Playing;
        _routine = null;
    }

    private IEnumerator CloseRoutine(bool finished)
    {
        _state = State.FadingOut;
        _pointerDragging = false;

        if (closeButton != null) closeButton.gameObject.SetActive(false);

        Tween lastFade = null;

        if (creditsText != null)
        {
            creditsText.DOKill();
            lastFade = creditsText.DOFade(0f, transitionDuration).SetUpdate(true);
        }

        if (loadingOverlay != null)
        {
            loadingOverlay.DOKill();
            lastFade = loadingOverlay.DOFade(0f, transitionDuration).SetUpdate(true);
        }

        if (lastFade != null) yield return lastFade.WaitForCompletion();

        if (loadingOverlay != null) loadingOverlay.blocksRaycasts = false;
        textRect.anchoredPosition = _startPosition;

        _state = State.Idle;
        _routine = null;

        if (finished || countAsWatchedWhenClosed)
            LevelEvents.RaiseCreditsWatched();
    }

    #endregion

    #region Scrolling

    private void Tick()
    {
        var y = textRect.anchoredPosition.y;

        var manual = allowManualScroll && HandleManualInput(ref y);
        if (manual)
        {
            _resumeTime = Time.unscaledTime + resumeDelay;
        }
        else if (!_pointerDragging && Time.unscaledTime >= _resumeTime)
        {
            y += scrollSpeed * Time.unscaledDeltaTime;
        }

        y = Mathf.Clamp(y, _startPosition.y, _endY);
        textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, y);

        if (y >= _endY && !_pointerDragging)
        {
            _routine = StartCoroutine(CloseRoutine(true));
        }
    }

    /// <returns>True if the player scrolled manually this frame.</returns>
    private bool HandleManualInput(ref float y)
    {
        var used = false;

        // Mouse wheel (wheel down = further in the credits)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Some platforms report 120 per notch, others 1 per notch.
                var notches = Mathf.Abs(scroll) >= 20f ? scroll / 120f : scroll;
                y -= notches * wheelStep;
                used = true;
            }
        }

        // Drag with finger or mouse: the text follows the pointer.
        var pointer = Pointer.current;
        if (pointer == null) return used;

        if (pointer.press.wasPressedThisFrame || (_pointerDragging && pointer != _dragPointer))
        {
            // New press, or another device took over (e.g. emulated mouse events) → no jump.
            _pointerDragging = pointer.press.isPressed &&
                               TryScreenToParentLocal(pointer.position.ReadValue(), out _lastPointerLocal);
            _dragPointer = pointer;
            return used;
        }

        if (!_pointerDragging) return used;

        if (!pointer.press.isPressed)
        {
            _pointerDragging = false;
            _resumeTime = Time.unscaledTime + resumeDelay;
            return used;
        }

        if (TryScreenToParentLocal(pointer.position.ReadValue(), out var local))
        {
            var dy = local.y - _lastPointerLocal.y;
            if (Mathf.Abs(dy) > 0.001f)
            {
                y += dy;
                used = true;
            }
            _lastPointerLocal = local;
        }

        return used;
    }

    private bool TryScreenToParentLocal(Vector2 screenPos, out Vector2 local)
    {
        var parent = textRect.parent as RectTransform;
        if (parent == null) { local = screenPos; return true; }

        Camera uiCamera = null;
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = _canvas.worldCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, uiCamera, out local);
    }

    /// <summary>
    /// anchoredPosition.y at which the lowest line of text has passed the top of the viewport.
    /// Uses the real rendered text bounds, so it also works if the text overflows its RectTransform.
    /// </summary>
    private float CalculateEndY()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

        var parent = textRect.parent as RectTransform;
        if (parent == null) return _startPosition.y;

        // Bottom of the content rect
        textRect.GetWorldCorners(_corners);
        var bottom = parent.InverseTransformPoint(_corners[0]).y;

        // Bottom of the actually rendered text (may be lower than the rect if it overflows)
        foreach (var text in textRect.GetComponentsInChildren<TMP_Text>(false))
        {
            text.ForceMeshUpdate();
            if (text.textInfo.characterCount == 0) continue;

            if (text.isTextTruncated)
            {
                Debug.LogWarning($"[CreditsScroller] '{text.name}' is truncated - part of the credits can't be shown. " +
                                 "Set its Overflow to 'Overflow' or add a ContentSizeFitter (Vertical Fit = Preferred Size).",
                    text);
            }

            var bounds = text.textBounds;
            var worldMin = text.rectTransform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, 0f));
            bottom = Mathf.Min(bottom, parent.InverseTransformPoint(worldMin).y);
        }

        // Top of the viewport
        var view = viewport;
        if (view == null && _canvas != null) view = _canvas.rootCanvas.transform as RectTransform;
        if (view == null) view = parent;

        view.GetWorldCorners(_corners);
        var top = parent.InverseTransformPoint(_corners[1]).y;

        return _startPosition.y + Mathf.Max(0f, top - bottom) + endPadding;
    }

    #endregion
}