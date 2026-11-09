using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates two effect materials and two styles, and makes "Glow" the default
/// (Resources/SelectionStyle.asset, used by every component that has no own style).
/// Existing assets are kept, so tweaked values are not lost when the menu is used again.
/// </summary>
public static class SelectionStyleCreator
{
    private const string Folder = "Assets/Materials/Selection";
    private const string DefaultStylePath = "Assets/Resources/SelectionStyle.asset";

    [MenuItem("Tools/RISCadventure/Create Selection Styles")]
    private static void Create()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Path.GetDirectoryName(DefaultStylePath));

        var glowMaterial = LoadOrCreateMaterial("RISCadventure/Component Highlight", $"{Folder}/Highlight Glow.mat");
        var hologramMaterial = LoadOrCreateMaterial("RISCadventure/Component Hologram", $"{Folder}/Hologram.mat");

        LoadOrCreateStyle($"{Folder}/Style Glow.asset", SelectionStyle.Mode.AddOverlay, glowMaterial);
        LoadOrCreateStyle($"{Folder}/Style Hologram.asset", SelectionStyle.Mode.ReplaceMaterial, hologramMaterial);
        var defaultStyle = LoadOrCreateStyle(DefaultStylePath, SelectionStyle.Mode.AddOverlay, glowMaterial);

        AssetDatabase.SaveAssets();
        Selection.activeObject = defaultStyle;
        Debug.Log("[SelectionStyleCreator] Done. The default style is Resources/SelectionStyle.asset – " +
                  "switch its Mode/Material to try the hologram.", defaultStyle);
    }

    private static Material LoadOrCreateMaterial(string shaderName, string path)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError($"[SelectionStyleCreator] Shader '{shaderName}' not found. Are the .shader files imported?");
            return null;
        }

        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static SelectionStyle LoadOrCreateStyle(string path, SelectionStyle.Mode mode, Material material)
    {
        var style = AssetDatabase.LoadAssetAtPath<SelectionStyle>(path);
        if (style != null) return style;

        style = ScriptableObject.CreateInstance<SelectionStyle>();
        style.mode = mode;
        style.material = material;
        AssetDatabase.CreateAsset(style, path);
        return style;
    }
}
