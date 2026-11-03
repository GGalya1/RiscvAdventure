using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 Animates values traveling along bus segments (LineRenderers).
 
 Levels describe an animation as a sequence of waves: all pulses of a wave start together,
 the next wave starts when every signal of the previous one has arrived
*/
public class BusController : MonoBehaviour
{
    [Header("UI Elements")] public Slider speedSlider;

    [Header("Motion settings")] public GameObject spherePrefab; // Sphere prefab (aka signal)

    public float movementSpeed = 7f;

    [Header("Pool settings")] [SerializeField]
    private int poolPreWarmCount = 5;

    private readonly HashSet<BusSignal> _activeSignals = new();
    private readonly Stack<BusSignal> _pool = new();

    // World-space path of every registered segment
    private readonly Dictionary<LineRenderer, Vector3[]> _busPaths = new();
    private readonly Dictionary<LineRenderer, Vector3[]> _reverseBusPaths = new();

    private BusSignal _spherePrefabSignal;
    private WaitUntil _waitUntilIdle;

    public bool NoActiveSignals => _activeSignals.Count == 0;

    private void Awake()
    {
        _spherePrefabSignal = spherePrefab.GetComponent<BusSignal>();
        _waitUntilIdle = new WaitUntil(() => NoActiveSignals);
    }

    private void Start()
    {
        PrewarmPool();
        spherePrefab.SetActive(false);

        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(OnSliderValueChanged);
            movementSpeed = speedSlider.value;
        }
        else
        {
            CustomLog.LogEditorError("Slider is null !");
        }

        CustomLog.LogEditor($"The controller has loaded {_busPaths.Count} bus segments.");
    }

    private void OnDestroy()
    {
        StopAllCoroutines();

        // pooled objects are not children of this object and must be removed manually
        foreach (var signal in _activeSignals)
            if (signal != null)
                Destroy(signal.gameObject);

        foreach (var signal in _pool)
            if (signal != null)
                Destroy(signal.gameObject);
    }

    #region Public API
    //  Starts all pulses at once and waits until every signal on the buses has arrived
    public IEnumerator Wave(params BusPulse[] pulses) => PlayWave(pulses, false);

    //  Same as Wave, but the signals travel from the end of each segment to its start
    public IEnumerator WaveBack(params BusPulse[] pulses) => PlayWave(pulses, true);

    // Plays the waves one after another
    public IEnumerator Play(IReadOnlyList<BusPulse[]> waves)
    {
        foreach (var wave in waves)
            yield return PlayWave(wave, false);
    }

    // Plays the waves in reverse order and every signal in reverse direction
    public IEnumerator PlayBackwards(IReadOnlyList<BusPulse[]> waves)
    {
        for (var i = waves.Count - 1; i >= 0; i--)
            yield return PlayWave(waves[i], true);
    }

    // Waits until no signal is travelling
    public IEnumerator WaitUntilIdle()
    {
        yield return _waitUntilIdle;
    }

    // Sends one value along targetBus without waiting
    public void StartBusSignal(LineRenderer targetBus, int value, bool reversedPath = false)
    {
        var pathPoints = (reversedPath ? _reverseBusPaths : _busPaths).GetValueOrDefault(targetBus);
        if (pathPoints == null || pathPoints.Length < 2) return;

        var signal = GetSignal(pathPoints[0]);
        signal.ShowValue(value);

        _activeSignals.Add(signal);
        StartCoroutine(MoveSphereAlongPath(signal, pathPoints));
    }

    // Pre-computes the world-space path of a segment
    public void RegisterSegment(LineRenderer lr)
    {
        if (lr == null)
        {
            CustomLog.LogEditorWarning("null LineRenderer passed - skipping.");
            return;
        }

        // Idempotent - skip if already registered
        if (_busPaths.ContainsKey(lr))
            return;

        var count = lr.positionCount;
        if (count < 2)
        {
            CustomLog.LogEditorWarning($" '{lr.gameObject.name}' has fewer than 2 vertices - skipping.");
            return;
        }

        // local positions -> world space
        var t = lr.transform;
        var points = new Vector3[count];
        lr.GetPositions(points);
        for (var i = 0; i < count; i++)
            points[i] = t.TransformPoint(points[i]);

        var reversed = new Vector3[count];
        Array.Copy(points, reversed, count);
        Array.Reverse(reversed);

        _busPaths[lr] = points;
        _reverseBusPaths[lr] = reversed;
    }
    #endregion

    private IEnumerator PlayWave(BusPulse[] pulses, bool reversed)
    {
        foreach (var pulse in pulses)
            StartBusSignal(pulse.Segment, pulse.Value, reversed);

        yield return _waitUntilIdle;
    }

    private IEnumerator MoveSphereAlongPath(BusSignal signal, Vector3[] pathPoints)
    {
        for (var i = 1; i < pathPoints.Length; i++)
        {
            var targetPoint = pathPoints[i];

            while (Vector3.Distance(signal.transform.position, targetPoint) > 0.001f)
            {
                signal.transform.position = Vector3.MoveTowards(
                    signal.transform.position,
                    targetPoint,
                    movementSpeed * Time.deltaTime
                );
                yield return null;
            }
        }

        ReturnSignal(signal);
    }

    private void OnSliderValueChanged(float value)
    {
        movementSpeed = value;
    }

    #region Pool
    private void PrewarmPool()
    {
        for (var i = 0; i < poolPreWarmCount; i++)
        {
            var signal = Instantiate(_spherePrefabSignal);
            signal.gameObject.SetActive(false);
            _pool.Push(signal);
        }
    }

    private BusSignal GetSignal(Vector3 startPosition)
    {
        var signal = _pool.Count > 0
            ? _pool.Pop()
            : Instantiate(_spherePrefabSignal);

        signal.transform.position = startPosition;
        signal.gameObject.SetActive(true);
        return signal;
    }

    private void ReturnSignal(BusSignal signal)
    {
        _activeSignals.Remove(signal);
        signal.ResetVisualisation();
        signal.gameObject.SetActive(false);
        _pool.Push(signal);
    }
    #endregion
}
