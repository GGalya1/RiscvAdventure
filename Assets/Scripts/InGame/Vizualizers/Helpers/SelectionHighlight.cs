using DG.Tweening;
using UnityEngine;

/// <summary>
/// Shows and hides the selection effect of one renderer according to a <see cref="SelectionStyle"/>.
/// <br/>The effect material is attached only while the component is selected, so unselected components render
/// exactly as before and the effect costs nothing on weak laptops.
/// </summary>
public sealed class SelectionHighlight
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int SelectTimeId = Shader.PropertyToID("_SelectTime");
    private static readonly int ScanRangeId = Shader.PropertyToID("_ScanRange");
    private static readonly int ShaderTimeId = Shader.PropertyToID("_Time");
    private static MaterialPropertyBlock _block;

    private readonly Renderer _renderer;
    private readonly SelectionStyle _style;
    private readonly Material[] _originalMaterials;
    private readonly Material[] _withOverlay;
    private readonly Material[] _replaced;

    private Tween _fade;
    private float _intensity;
    private bool _fallbackToTint;

    public SelectionHighlight(Renderer renderer, SelectionStyle style)
    {
        _renderer = renderer;
        _style = style;
        if (_renderer == null) return;

        _originalMaterials = _renderer.sharedMaterials;
        if (_style == null || _style.material == null) return;

        // Static batching merges the meshes of all static objects with the same material into one mesh.
        // An extra material then redraws the LAST sub-mesh of that merged mesh – a different object.
        if (_style.mode == SelectionStyle.Mode.AddOverlay && _renderer.isPartOfStaticBatch)
        {
            CustomLog.LogEditorWarning($"[SelectionHighlight] '{_renderer.name}' is 'Batching Static'. Untick it in the prefab, " +
                                       "otherwise the overlay appears on another object. Using the colour tint instead.");
            _fallbackToTint = true;
            return;
        }

        // An extra material on a renderer is drawn again over the last sub-mesh – that is the overlay.
        _withOverlay = new Material[_originalMaterials.Length + 1];
        _originalMaterials.CopyTo(_withOverlay, 0);
        _withOverlay[_originalMaterials.Length] = _style.material;

        _replaced = new Material[_originalMaterials.Length];
        for (var i = 0; i < _replaced.Length; i++) _replaced[i] = _style.material;
    }

    public bool TintsModel => _style == null || _style.mode == SelectionStyle.Mode.TintOnly || _style.tintModel || _fallbackToTint;

    public void Show()
    {
        if (!HasEffect) return;
        _fade?.Kill();

        if (_style.mode == SelectionStyle.Mode.ReplaceMaterial)
        {
            // A cross-fade between an opaque and a transparent material is not possible without a second draw,
            // so the replacement is switched instantly.
            _renderer.sharedMaterials = _replaced;
            SetIntensity(1f);
            return;
        }

        _renderer.sharedMaterials = _withOverlay;
        StartSelectAnimation();
        _fade = DOTween.To(() => _intensity, SetIntensity, 1f, _style.fadeDuration).SetEase(Ease.OutCubic);
    }

    public void Hide()
    {
        if (!HasEffect) return;
        _fade?.Kill();

        if (_style.mode == SelectionStyle.Mode.ReplaceMaterial || _style.fadeDuration <= 0f)
        {
            Restore();
            return;
        }

        _fade = DOTween.To(() => _intensity, SetIntensity, 0f, _style.fadeDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(Restore);
    }

    /// <summary> Removes the effect at once, e.g. when the object is destroyed or the panel closes without animation. </summary>
    public void HideImmediately()
    {
        if (!HasEffect) return;
        _fade?.Kill();
        Restore();
    }

    private bool HasEffect =>
        _renderer != null && _withOverlay != null && _style.mode != SelectionStyle.Mode.TintOnly;

    private void Restore()
    {
        SetIntensity(0f);
        if (_renderer != null) _renderer.sharedMaterials = _originalMaterials;
    }

    /// <summary>
    /// Tells the shader when the selection happened and where the model starts and ends along the diagonal
    /// (x + z) – the light band sweeps over it once and the pulse fades out from this moment.
    /// </summary>
    private void StartSelectAnimation()
    {
        var bounds = _renderer.bounds;
        _block ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_block);
        // Read the clock the render pipeline gives to shaders (_Time.y) instead of guessing it:
        // URP fills it from Time.time, not from Time.timeSinceLevelLoad.
        _block.SetFloat(SelectTimeId, Shader.GetGlobalVector(ShaderTimeId).y);
        _block.SetVector(ScanRangeId, new Vector4(bounds.min.x + bounds.min.z, bounds.max.x + bounds.max.z, 0f, 0f));
        _renderer.SetPropertyBlock(_block);
    }

    // Through the property block (like RendererColor), so the shared material asset is never changed.
    private void SetIntensity(float value)
    {
        _intensity = value;
        if (_renderer == null) return;

        _block ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_block);
        _block.SetFloat(IntensityId, value);
        _renderer.SetPropertyBlock(_block);
    }
}
