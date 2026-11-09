using UnityEngine;

/// <summary>
/// Look of a signal that runs along a wire: spark head (quad with the Electric Spark shader),
/// flying sparks (ParticleSystem) and a short tail (TrailRenderer). All parts are optional.
/// <br/>Signals are pooled by <see cref="BusController"/>: a signal disappears at the end of one wire and is
/// re-used at the start of another. Without cleaning, the tail and the sparks would draw a line across the
/// whole level from the old place to the new one, so everything is reset when the signal is switched on.
/// </summary>
public class SparkSignal : MonoBehaviour
{
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static MaterialPropertyBlock _block;

    [SerializeField] private Renderer sparkRenderer;
    [SerializeField] private ParticleSystem flyingSparks;
    [SerializeField] private TrailRenderer trail;

    // BusController sets the position BEFORE it activates the signal, so OnEnable already sees the new place.
    private void OnEnable()
    {
        if (trail != null) trail.Clear();

        if (flyingSparks != null)
        {
            flyingSparks.Clear(true);
            flyingSparks.Play(true);
        }

        // Every signal gets its own random arc shapes, otherwise all sparks on screen would look identical.
        if (sparkRenderer != null)
        {
            _block ??= new MaterialPropertyBlock();
            sparkRenderer.GetPropertyBlock(_block);
            _block.SetFloat(SeedId, Random.Range(0f, 1000f));
            sparkRenderer.SetPropertyBlock(_block);
        }
    }

    private void OnDisable()
    {
        if (flyingSparks != null) flyingSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}