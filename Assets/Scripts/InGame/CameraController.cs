using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Manages horizontal camera movement with smoothing, boundaries, and UI/Interaction checks.
/// Supports both Mouse and Touchscreen input.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Pan")]
    [Tooltip("1 = the grabbed point follows the finger/cursor exactly. <1 slower, >1 faster.")]
    [SerializeField, Range(0.1f, 3f)] private float panSpeed = 1f;

    [Tooltip("Smoothing while the finger/mouse is held (seconds). Small values feel 'glued' to the finger.")]
    [SerializeField, Range(0f, 0.3f)] private float dragSmoothTime = 0.04f;

    [Tooltip("Smoothing for zoom steps and after releasing (seconds).")]
    [SerializeField, Range(0f, 0.5f)] private float releaseSmoothTime = 0.12f;

    [Tooltip("How far (in density-independent pixels, 160 dpi) the pointer has to move before a drag starts. " +
             "Prevents taps and finger jitter from moving the camera.")]
    [SerializeField, Min(0f)] private float dragThresholdDp = 8f;

    [Tooltip("If enabled, a drag may also start on a ClickableObject (a tap still opens it). " +
             "If disabled, pressing a ClickableObject never moves the camera.")]
    [SerializeField] private bool allowDragFromClickableObjects = true;

    [Header("Inertia (after releasing a drag)")]
    [SerializeField] private bool useInertia = true;

    [Tooltip("How quickly the glide stops. Bigger = shorter glide.")]
    [SerializeField, Min(0.1f)] private float inertiaDamping = 8f;

    [Tooltip("Release speeds below this (world units/s) don't glide.")]
    [SerializeField, Min(0f)] private float minInertiaSpeed = 1f;

    [Tooltip("Upper limit for the glide speed (world units/s).")]
    [SerializeField, Min(0f)] private float maxInertiaSpeed = 60f;

    [Header("Zoom")]
    [Tooltip("Fraction of the current height zoomed per mouse wheel notch.")]
    [SerializeField, Range(0.01f, 0.5f)] private float scrollZoomStep = 0.12f;

    [Tooltip("1 = content scales exactly with the fingers.")]
    [SerializeField, Range(0.1f, 3f)] private float pinchZoomSpeed = 1f;

    [Tooltip("Zoom towards the cursor / pinch centre instead of the screen centre.")]
    [SerializeField] private bool zoomTowardsPointer = true;

    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 20f;

    [Header("Bounds (Horizontal)")]
    public float minX;
    public float maxX;
    public float minZ;
    public float maxZ;

    [Header("Drag Plane")]
    [Tooltip("Y of the plane that is 'grabbed' when nothing with a collider is under the pointer.")]
    [SerializeField] private float groundHeight = 0f;

    [Tooltip("On start, set Ground Height from a raycast through the screen centre (if it hits a collider).")]
    [SerializeField] private bool autoDetectGroundHeight = true;

    [Header("References")]
    [SerializeField] private Camera cam;

    // Touch → mouse emulation guard (mobile browsers send fake mouse events after touches).
    private const float MouseIgnoreAfterTouch = 0.5f;
    // If the finger rested longer than this before release, don't glide.
    private const float InertiaMaxRestTime = 0.08f;
    private const float VelocityFilterRate = 12f;

    private enum Gesture { None, Pending, Panning, Pinching, Blocked }

    private Gesture _gesture = Gesture.None;
    private Transform _cachedTransform;
    private Vector3 _targetPosition;
    private Vector3 _smoothVelocity;
    private Vector3 _panVelocity;      // measured while dragging
    private Vector3 _inertiaVelocity;  // applied after release
    private float _dragPlaneY;
    private float _lastMoveTime;

    private Vector2 _pressPosition;
    private Vector2 _lastPointerPosition;
    private int _panTouchId = -1;

    private int _pinchIdA = -1, _pinchIdB = -1;
    private float _lastPinchDistance;
    private Vector2 _lastPinchCenter;

    private bool _touchGestureActive;
    private bool _mouseGestureActive;
    private float _lastTouchTime = float.NegativeInfinity;

    private readonly List<TouchControl> _activeTouches = new List<TouchControl>(4);
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(8);
    private PointerEventData _pointerData;
    private EventSystem _pointerDataOwner;

    private static bool _staticDragging;
    private static int _lastDragEndFrame = -10;

    /// <summary>
    /// True while the camera is being dragged/pinched and for one frame after.
    /// Lets click handlers ignore the release at the end of a drag.
    /// </summary>
    public static bool IsDragInProgressOrJustEnded =>
        _staticDragging || Time.frameCount - _lastDragEndFrame <= 1;

    private void Awake()
    {
        _cachedTransform = transform;
        _targetPosition = _cachedTransform.position;
        if (cam == null) cam = Camera.main;
    }

    private void Start()
    {
        if (autoDetectGroundHeight && cam != null)
        {
            var ray = cam.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            if (Physics.Raycast(ray, out var hit, cam.farClipPlane))
                groundHeight = hit.point.y;
        }
        _dragPlaneY = groundHeight;
    }

    private void OnDisable()
    {
        ResetGesture();
        _inertiaVelocity = Vector3.zero;
        _touchGestureActive = false;
        _mouseGestureActive = false;
    }

    private void OnValidate()
    {
        if (maxHeight < minHeight) maxHeight = minHeight;
        if (maxX < minX) maxX = minX;
        if (maxZ < minZ) maxZ = minZ;
    }

    private void Update()
    {
        if (cam == null) return;

        var touchUsed = HandleTouch();
        if (!touchUsed) HandleMouse();

        ApplyInertia();
        ApplyMovement();

        _staticDragging = _gesture == Gesture.Panning || _gesture == Gesture.Pinching;
    }

    #region Touch

    /// <returns>True if touch input is active this frame (mouse is then ignored).</returns>
    private bool HandleTouch()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return false;

        _activeTouches.Clear();
        foreach (var touch in touchscreen.touches)
        {
            if (touch.isInProgress) _activeTouches.Add(touch);
        }

        if (_activeTouches.Count == 0)
        {
            if (_touchGestureActive)
            {
                EndGesture();
                _touchGestureActive = false;
            }
            return false;
        }

        _lastTouchTime = Time.unscaledTime;

        if (!_touchGestureActive)
        {
            // Mouse could have started something — touch takes over.
            if (_mouseGestureActive) { ResetGesture(); _mouseGestureActive = false; }

            _touchGestureActive = true;
            var first = _activeTouches[0];
            BeginGesture(first.position.ReadValue(), first.touchId.ReadValue());
        }

        if (_gesture == Gesture.Blocked) return true;

        if (_activeTouches.Count >= 2)
        {
            HandlePinch(_activeTouches[0], _activeTouches[1]);
            return true;
        }

        var t = _activeTouches[0];
        var id = t.touchId.ReadValue();
        var pos = t.position.ReadValue();

        if (_gesture == Gesture.Pinching || id != _panTouchId)
        {
            // One finger left after a pinch, or a different finger is now leading:
            // continue from its current position so the camera doesn't jump.
            _panTouchId = id;
            _lastPointerPosition = pos;
            if (_gesture == Gesture.Pinching) _gesture = Gesture.Panning;
            else if (_gesture == Gesture.Pending) _pressPosition = pos;
            _panVelocity = Vector3.zero;
            return true;
        }

        UpdateDrag(pos);
        return true;
    }

    private void HandlePinch(TouchControl a, TouchControl b)
    {
        var idA = a.touchId.ReadValue();
        var idB = b.touchId.ReadValue();
        var posA = a.position.ReadValue();
        var posB = b.position.ReadValue();
        var center = (posA + posB) * 0.5f;
        var distance = Vector2.Distance(posA, posB);

        if (_gesture != Gesture.Pinching || idA != _pinchIdA || idB != _pinchIdB)
        {
            // Pinch (re)starts: just remember the state, no movement this frame.
            _gesture = Gesture.Pinching;
            _pinchIdA = idA;
            _pinchIdB = idB;
            _lastPinchDistance = distance;
            _lastPinchCenter = center;
            _dragPlaneY = GetDragPlaneHeight(center);
            _panVelocity = Vector3.zero;
            return;
        }

        // Two-finger pan with the pinch centre.
        PanByScreenDelta(_lastPinchCenter, center);

        // Zoom: ratio of finger distances → resolution/DPI independent.
        if (distance > 1f && _lastPinchDistance > 1f)
        {
            var factor = Mathf.Pow(_lastPinchDistance / distance, pinchZoomSpeed);
            ZoomAt(center, factor, _dragPlaneY);
        }

        _lastPinchDistance = distance;
        _lastPinchCenter = center;
        _panVelocity = Vector3.zero; // no glide after pinching
    }

    #endregion

    #region Mouse

    private void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Ignore emulated mouse events that follow a touch.
        if (Time.unscaledTime - _lastTouchTime < MouseIgnoreAfterTouch) return;

        var pos = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _mouseGestureActive = true;
            BeginGesture(pos, -1);
        }

        if (_mouseGestureActive)
        {
            if (mouse.leftButton.isPressed)
            {
                if (_gesture != Gesture.Blocked) UpdateDrag(pos);
            }
            else
            {
                EndGesture();
                _mouseGestureActive = false;
            }
        }

        var scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f && !IsPointerOverUI(pos))
        {
            // Some platforms report 120 per notch, others 1 per notch.
            var notches = Mathf.Abs(scroll) >= 20f ? scroll / 120f : scroll;
            notches = Mathf.Clamp(notches, -3f, 3f);

            var factor = Mathf.Pow(1f - scrollZoomStep, notches); // wheel up → zoom in
            var planeY = _gesture == Gesture.Panning ? _dragPlaneY : GetDragPlaneHeight(pos);
            ZoomAt(pos, factor, planeY);
        }
    }

    #endregion

    #region Gesture logic (shared by touch and mouse)

    private void BeginGesture(Vector2 screenPos, int touchId)
    {
        var wasGliding = _inertiaVelocity.sqrMagnitude > 0f;
        _inertiaVelocity = Vector3.zero;
        _panVelocity = Vector3.zero;

        if (IsPointerOverUI(screenPos) ||
            (!allowDragFromClickableObjects && IsClickingInteractiveObject(screenPos)))
        {
            _gesture = Gesture.Blocked;
            return;
        }

        // "Catch" a gliding camera where it currently is.
        if (wasGliding)
        {
            _targetPosition = _cachedTransform.position;
            _smoothVelocity = Vector3.zero;
        }

        _gesture = Gesture.Pending;
        _pressPosition = screenPos;
        _lastPointerPosition = screenPos;
        _panTouchId = touchId;
        _dragPlaneY = GetDragPlaneHeight(screenPos);
    }

    private void UpdateDrag(Vector2 screenPos)
    {
        if (_gesture == Gesture.Pending)
        {
            var threshold = DragThresholdPixels();
            if ((screenPos - _pressPosition).sqrMagnitude < threshold * threshold) return;

            _gesture = Gesture.Panning;
            _lastPointerPosition = screenPos;
            _lastMoveTime = Time.unscaledTime;
            return;
        }

        if (_gesture != Gesture.Panning) return;

        var applied = Vector3.zero;
        if (screenPos != _lastPointerPosition)
        {
            applied = PanByScreenDelta(_lastPointerPosition, screenPos);
            _lastPointerPosition = screenPos;
            _lastMoveTime = Time.unscaledTime;
        }
        TrackVelocity(applied);
    }

    private void EndGesture()
    {
        if (_gesture == Gesture.Panning && useInertia &&
            Time.unscaledTime - _lastMoveTime <= InertiaMaxRestTime)
        {
            var v = Vector3.ClampMagnitude(_panVelocity, maxInertiaSpeed);
            if (v.magnitude >= minInertiaSpeed) _inertiaVelocity = v;
        }

        if (_gesture == Gesture.Panning || _gesture == Gesture.Pinching)
            _lastDragEndFrame = Time.frameCount;

        ResetGesture();
    }

    private void ResetGesture()
    {
        _gesture = Gesture.None;
        _panTouchId = -1;
        _pinchIdA = _pinchIdB = -1;
        _panVelocity = Vector3.zero;
        _staticDragging = false;
    }

    #endregion

    #region Movement

    /// <summary>
    /// Moves the target so that the world point under <paramref name="from"/> ends up under <paramref name="to"/>.
    /// </summary>
    /// <returns>The world-space offset that was actually applied (after clamping).</returns>
    private Vector3 PanByScreenDelta(Vector2 from, Vector2 to)
    {
        Vector3 worldDelta;

        if (TryScreenToPlane(from, _targetPosition, _dragPlaneY, out var a) &&
            TryScreenToPlane(to, _targetPosition, _dragPlaneY, out var b))
        {
            worldDelta = a - b;
        }
        else
        {
            // Fallback (pointer above the horizon): scale pixels by the visible height.
            var screenDelta = to - from;
            var unitsPerPixel = VisibleWorldHeight() / Mathf.Max(1, Screen.height);
            worldDelta = new Vector3(-screenDelta.x, 0f, -screenDelta.y) * unitsPerPixel;
        }

        worldDelta.y = 0f;
        worldDelta *= panSpeed;

        var before = _targetPosition;
        _targetPosition = ClampToBounds(_targetPosition + worldDelta);
        return _targetPosition - before;
    }

    /// <summary>
    /// Zooms by <paramref name="factor"/> (&lt;1 = closer) along the view direction,
    /// optionally keeping the point under <paramref name="screenPos"/> fixed.
    /// </summary>
    private void ZoomAt(Vector2 screenPos, float factor, float planeY)
    {
        var pos = _targetPosition;

        var anchorBefore = Vector3.zero;
        var hasAnchor = zoomTowardsPointer && TryScreenToPlane(screenPos, pos, planeY, out anchorBefore);

        var heightAbovePlane = pos.y - planeY;
        var desiredY = heightAbovePlane > 0.01f ? planeY + heightAbovePlane * factor : pos.y * factor;
        desiredY = Mathf.Clamp(desiredY, minHeight, maxHeight);

        var dy = desiredY - pos.y;
        if (Mathf.Abs(dy) < 1e-5f) return;

        // Move along the view direction (like before), but land exactly on the clamped height.
        var forward = cam.transform.forward;
        if (forward.y < -0.05f) pos += forward * (dy / forward.y);
        else pos.y += dy;

        if (hasAnchor && TryScreenToPlane(screenPos, pos, planeY, out var anchorAfter))
        {
            pos.x += anchorBefore.x - anchorAfter.x;
            pos.z += anchorBefore.z - anchorAfter.z;
        }

        _targetPosition = ClampToBounds(pos);
    }

    private void TrackVelocity(Vector3 appliedDelta)
    {
        var dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        var instant = appliedDelta / dt;
        _panVelocity = Vector3.Lerp(_panVelocity, instant, 1f - Mathf.Exp(-VelocityFilterRate * dt));
    }

    private void ApplyInertia()
    {
        if (_gesture != Gesture.None && _gesture != Gesture.Blocked) return;
        if (_inertiaVelocity.sqrMagnitude <= 0f) return;

        var dt = Time.unscaledDeltaTime;
        var unclamped = _targetPosition + _inertiaVelocity * dt;
        _targetPosition = ClampToBounds(unclamped);

        // Stop gliding along an axis that hit the bounds.
        if (!Mathf.Approximately(unclamped.x, _targetPosition.x)) _inertiaVelocity.x = 0f;
        if (!Mathf.Approximately(unclamped.z, _targetPosition.z)) _inertiaVelocity.z = 0f;

        _inertiaVelocity *= Mathf.Exp(-inertiaDamping * dt);
        if (_inertiaVelocity.magnitude < minInertiaSpeed * 0.1f) _inertiaVelocity = Vector3.zero;
    }

    private void ApplyMovement()
    {
        var dt = Time.unscaledDeltaTime;
        var dragging = _gesture == Gesture.Panning || _gesture == Gesture.Pinching;
        var smoothTime = dragging ? dragSmoothTime : releaseSmoothTime;

        if (smoothTime <= 0f)
        {
            _cachedTransform.position = _targetPosition;
            _smoothVelocity = Vector3.zero;
            return;
        }
        if (dt <= 0f) return;

        _cachedTransform.position = Vector3.SmoothDamp(
            _cachedTransform.position, _targetPosition, ref _smoothVelocity, smoothTime, Mathf.Infinity, dt);
    }

    private Vector3 ClampToBounds(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.z = Mathf.Clamp(p.z, minZ, maxZ);
        return p;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Intersects the ray through <paramref name="screenPos"/> with the plane y = <paramref name="planeY"/>,
    /// as if this object were at <paramref name="rigPosition"/> (rotation stays the same).
    /// </summary>
    private bool TryScreenToPlane(Vector2 screenPos, Vector3 rigPosition, float planeY, out Vector3 point)
    {
        point = default;
        var ray = cam.ScreenPointToRay(screenPos);
        var origin = ray.origin + (rigPosition - _cachedTransform.position);
        var dir = ray.direction;

        if (Mathf.Abs(dir.y) < 1e-4f) return false;
        var t = (planeY - origin.y) / dir.y;
        if (t <= 0f) return false;

        point = origin + dir * t;
        return true;
    }

    /// <summary>
    /// Height of the surface under the pointer (so exactly what was touched is dragged),
    /// falling back to <see cref="groundHeight"/>.
    /// </summary>
    private float GetDragPlaneHeight(Vector2 screenPos)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, cam.farClipPlane) && hit.point.y < _targetPosition.y - 0.5f)
            return hit.point.y;
        return groundHeight;
    }

    private float VisibleWorldHeight()
    {
        if (cam.orthographic) return cam.orthographicSize * 2f;
        var distance = Mathf.Max(0.01f, _targetPosition.y - _dragPlaneY);
        return 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    private float DragThresholdPixels()
    {
        var dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        return dragThresholdDp * dpi / 160f;
    }

    /// <summary>
    /// Checks if the pointer is over a specific interactive 3D object.
    /// </summary>
    private bool IsClickingInteractiveObject(Vector2 screenPos)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out var hit) && hit.collider.TryGetComponent<ClickableObject>(out _);
    }

    /// <summary>
    /// Raycasts the UI directly at the given position. Unlike IsPointerOverGameObject this also
    /// works in the very frame a touch begins. 3D objects hit by a PhysicsRaycaster don't count as UI.
    /// </summary>
    private bool IsPointerOverUI(Vector2 screenPos)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        if (_pointerData == null || _pointerDataOwner != eventSystem)
        {
            _pointerData = new PointerEventData(eventSystem);
            _pointerDataOwner = eventSystem;
        }

        _pointerData.position = screenPos;
        _uiRaycastResults.Clear();
        eventSystem.RaycastAll(_pointerData, _uiRaycastResults);

        foreach (var result in _uiRaycastResults)
        {
            if (!(result.module is PhysicsRaycaster)) return true;
        }
        return false;
    }

    #endregion
}