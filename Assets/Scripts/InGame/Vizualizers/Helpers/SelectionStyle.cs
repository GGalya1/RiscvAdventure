using UnityEngine;

/// <summary>
/// How a component looks while it is selected (its info panel is open).
/// <br/>The default style is <c>Resources/SelectionStyle.asset</c>; a visualizer can override it in the Inspector.
/// Create the assets with Tools → RISCadventure → Create Selection Styles.
/// </summary>
[CreateAssetMenu(fileName = "SelectionStyle", menuName = "RISCadventure/Selection Style")]
public class SelectionStyle : ScriptableObject
{
    public const string ResourcePath = "SelectionStyle";

    public enum Mode
    {
        /// <summary> Only the colour changes (the old behaviour). </summary>
        TintOnly,
        /// <summary> The effect material is drawn on top of the normal one (outline, glow). </summary>
        AddOverlay,
        /// <summary> The effect material replaces the normal one (hologram, glass). </summary>
        ReplaceMaterial
    }

    [Tooltip("AddOverlay: e.g. 'Component Highlight'. ReplaceMaterial: e.g. 'Component Hologram'.")]
    public Mode mode = Mode.AddOverlay;

    [Tooltip("Material with a RISCadventure selection shader.")]
    public Material material;

    [Tooltip("Also tint the model with the visualizer's Selection Color, as before.")]
    public bool tintModel;

    [Tooltip("Fade in/out of the effect (AddOverlay only), seconds.")]
    [Min(0f)] public float fadeDuration = 0.2f;

    private static SelectionStyle _default;
    private static bool _defaultLoaded;

    /// <summary> Resources/SelectionStyle.asset, or null (then only the colour changes). </summary>
    public static SelectionStyle Default
    {
        get
        {
            if (_defaultLoaded) return _default;
            _defaultLoaded = true;
            _default = Resources.Load<SelectionStyle>(ResourcePath);
            return _default;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _default = null;
        _defaultLoaded = false;
    }
}
