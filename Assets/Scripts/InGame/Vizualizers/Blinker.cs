using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class Blinker : MonoBehaviour
{
    [FormerlySerializedAs("_targetRenderer")] [SerializeField] private Renderer targetRenderer;
    [FormerlySerializedAs("_blinkColor")] [SerializeField] private Color blinkColor = Color.yellow;
    [FormerlySerializedAs("_duration")] [SerializeField] private float duration = 1.0f;

    private Color _originalColor;
    private Coroutine _coroutine;
    private WaitForSeconds _wait;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

        _wait = new WaitForSeconds(duration);
        _originalColor = RendererColor.Original(targetRenderer);
    }

    public void Trigger()
    {
        if (targetRenderer == null) return;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(BlinkSequence());
    }

    private IEnumerator BlinkSequence()
    {
        RendererColor.Set(targetRenderer, blinkColor);

        yield return _wait;

        RendererColor.Set(targetRenderer, _originalColor);
        _coroutine = null;
    }
}