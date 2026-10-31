using UnityEngine;

/*
 Recolors a renderer through a MaterialPropertyBlock (no material instances are created).
 Shared by every visualizer that tints a model, a bus line or a blink target.
*/
public static class RendererColor
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock _block;

    public static void Set(Renderer target, Color color)
    {
        if (target == null) return;

        _block ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        target.SetPropertyBlock(_block);
    }

    // The color of the shared material, i.e. the color to return to after a highlight. </summary>
    public static Color Original(Renderer target)
    {
        return target != null ? target.sharedMaterial.color : default;
    }
}