using DG.Tweening;
using UnityEngine;

/*
 Show/hide animation of a component's info panel: the panel fades in while sliding up
 to its authored position and fades out while sliding down.
*/
public sealed class PanelAnimator
{
    private readonly Transform _panel;
    private readonly CanvasGroup _canvasGroup;
    private readonly float _duration;
    private readonly Vector3 _shownPosition;
    private readonly Vector3 _hiddenPosition;

    public PanelAnimator(Transform panel, CanvasGroup canvasGroup, float duration, float moveDistance)
    {
        _panel = panel;
        _canvasGroup = canvasGroup;
        _duration = duration;
        _shownPosition = panel.localPosition;
        _hiddenPosition = _shownPosition - Vector3.up * moveDistance;
    }

    public void Show()
    {
        Kill();

        _panel.DOLocalMove(_shownPosition, _duration).SetEase(Ease.OutCubic);
        _canvasGroup.DOFade(1f, _duration).SetEase(Ease.OutCubic).OnComplete(() => SetInputEnabled(true));
    }

    public void Hide()
    {
        SetInputEnabled(false);
        Kill();

        _canvasGroup.DOFade(0f, _duration).SetEase(Ease.InCubic);
        _panel.DOLocalMove(_hiddenPosition, _duration).SetEase(Ease.InCubic);
    }

    // Hide without animation
    public void HideImmediately()
    {
        Kill();

        _canvasGroup.alpha = 0;
        SetInputEnabled(false);
        _panel.localPosition = _hiddenPosition;
    }

    private void Kill()
    {
        _panel.DOKill();
        _canvasGroup.DOKill();
    }

    private void SetInputEnabled(bool value)
    {
        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value;
    }
}