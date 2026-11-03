using System.Collections;
using DG.Tweening;
using UnityEngine;

//  "Wrong answer" animation: the cross pops up, shakes and collapses
public sealed class WrongAnswerFeedback
{
    private readonly GameObject _crossIndicator;
    private readonly float _displayDuration;

    public WrongAnswerFeedback(GameObject crossIndicator, float displayDuration)
    {
        _crossIndicator = crossIndicator;
        _displayDuration = displayDuration;
    }

    public IEnumerator Play()
    {
        if (_crossIndicator == null) yield break;

        var cross = _crossIndicator.transform;
        _crossIndicator.SetActive(true);
        cross.localScale = Vector3.zero;

        var sequence = DOTween.Sequence();

        // sudden appearance followed by a jolt
        sequence.Append(cross.DOScale(1.2f, 0.15f).SetEase(Ease.OutBack));
        sequence.Join(cross.DOShakePosition(0.3f, strength: 15f, vibrato: 15));

        // short pause so the player notices the mistake
        sequence.AppendInterval(_displayDuration);

        // collapse
        sequence.Append(cross.DOScale(0f, 0.15f).SetEase(Ease.InBack));

        yield return sequence.WaitForCompletion();

        _crossIndicator.SetActive(false);
    }
}