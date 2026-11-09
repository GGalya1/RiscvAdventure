using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes "smoothed" normals into UV3 of the component models for the outline of the selection shader.
/// <br/>The MagicaVoxel models have flat normals: at a cube corner three vertices sit on the same spot with three
/// different normals. If the outline pushes each of them along its own normal, the corner tears open.
/// Here every vertex gets the average of the DIFFERENT normals at its position (e.g. the diagonal at a corner),
/// so all copies of a corner move together and the outline stays closed. The lighting still uses the real normals.
/// <br/>Runs on import. After adding this file: right click Models/HardwareModels → Reimport.
/// </summary>
public class SmoothNormalsPostprocessor : AssetPostprocessor
{
    private const string Folder = "Assets/Models/HardwareModels/";
    private const int UvChannel = 3;            // TEXCOORD3 in the shader
    private const float PositionPrecision = 10000f;

    // Changing the number re-imports the models with the new code.
    public override uint GetVersion() => 1;

    private void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.StartsWith(Folder)) return;

        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null) Bake(filter.sharedMesh);
        }
    }

    private static void Bake(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        if (normals == null || normals.Length != vertices.Length) return;

        // Distinct normals per position (a flat face split into several quads must not count twice).
        var normalsAtPosition = new Dictionary<Vector3Int, List<Vector3>>();
        for (var i = 0; i < vertices.Length; i++)
        {
            var key = Key(vertices[i]);
            if (!normalsAtPosition.TryGetValue(key, out var list))
            {
                list = new List<Vector3>(3);
                normalsAtPosition[key] = list;
            }

            var isNew = true;
            foreach (var known in list)
            {
                if (Vector3.Dot(known, normals[i]) > 0.999f) { isNew = false; break; }
            }
            if (isNew) list.Add(normals[i]);
        }

        var smooth = new Vector3[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var sum = Vector3.zero;
            foreach (var normal in normalsAtPosition[Key(vertices[i])]) sum += normal;
            smooth[i] = sum.sqrMagnitude > 0.000001f ? sum.normalized : normals[i];
        }

        mesh.SetUVs(UvChannel, smooth);
    }

    private static Vector3Int Key(Vector3 position) => new(
        Mathf.RoundToInt(position.x * PositionPrecision),
        Mathf.RoundToInt(position.y * PositionPrecision),
        Mathf.RoundToInt(position.z * PositionPrecision));
}
