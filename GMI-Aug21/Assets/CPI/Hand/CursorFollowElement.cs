using UnityEngine;

public sealed class CursorFollowElement : MonoBehaviour
{
    private static readonly int PointerDownBool = Animator.StringToHash("PointerDown");
    private static readonly int PointerUpBool = Animator.StringToHash("PointerUp");

    private const string InterWaveLayerName = "InterWave";
    private const string WaveLayerName = "Wave";

    [SerializeField] private Animator _animator;
    [SerializeField] private Camera _canvasCamera;

    [Header("Follow Smoothing")]
    [Tooltip("Time (seconds) to catch up to the cursor. Higher = smoother/laggier, lower = snappier.")]
    [SerializeField] private float _smoothTime = 0.08f;
    [Tooltip("Maximum move speed in local units/sec, so fast mouse flicks don't cause a huge jump.")]
    [SerializeField] private float _maxSpeed = 4000f;
    [Tooltip("Ignore cursor deltas smaller than this many local units to prevent jitter from tiny mouse movements.")]
    [SerializeField] private float _deadZone = 0.5f;

    private RectTransform _rectTransform;
    private RectTransform _parentRectTransform;
    private Vector2 _followVelocity;
    private Vector2? _targetPoint;
    private bool _animatorNeedsSync;
    private bool _waveMode;

    private bool _worldOffsetFollowActive;
    private Transform _worldFollowTarget;
    private Camera _worldFollowCamera;
    private Vector2 _worldFollowOffset;

    /// <summary>
    /// Switches from following the cursor to following Target's projected position, offset by however far
    /// this element currently is from that projected position (so it doesn't jump on the switch) — used
    /// while dragging the planet, whose motion is clamped/eased and so can lag behind the raw cursor. The
    /// cursor is not read at all while this is active — the element only ever moves because Target moves.
    /// Call EndWorldOffsetFollow to resume plain cursor following; because both modes feed the same
    /// SmoothDamp'd _targetPoint, the switch back eases smoothly rather than snapping.
    /// </summary>
    public void BeginWorldOffsetFollow(Transform target, Camera worldCamera)
    {
        if (target == null || worldCamera == null || _rectTransform == null || _parentRectTransform == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRectTransform, worldCamera.WorldToScreenPoint(target.position), _canvasCamera, out var targetLocalPoint))
            return;

        _worldFollowOffset = _rectTransform.anchoredPosition - targetLocalPoint;
        _worldFollowTarget = target;
        _worldFollowCamera = worldCamera;
        _worldOffsetFollowActive = true;
    }

    public void EndWorldOffsetFollow()
    {
        _worldOffsetFollowActive = false;
        _worldFollowTarget = null;
        _worldFollowCamera = null;
    }

    /// <summary>
    /// Picks which animator layer plays: the wave layer (pointer down/up as a scale press) during a wave, the
    /// inter-wave layer (the hand poses) otherwise. Safe to call while the hand is hidden; a re-enabled
    /// Animator restarts with its default layer weights, so the choice is re-applied on the next Update.
    /// </summary>
    public void SetWaveMode(bool waveMode)
    {
        _waveMode = waveMode;

        if (isActiveAndEnabled)
            ApplyLayerWeights();
    }

    private void ApplyLayerWeights()
    {
        if (_animator == null) return;

        int interWaveLayer = _animator.GetLayerIndex(InterWaveLayerName);
        int waveLayer = _animator.GetLayerIndex(WaveLayerName);
        if (interWaveLayer < 0 || waveLayer < 0) return;

        _animator.SetLayerWeight(interWaveLayer, _waveMode ? 0f : 1f);
        _animator.SetLayerWeight(waveLayer, _waveMode ? 1f : 0f);
    }

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _parentRectTransform = _rectTransform != null ? _rectTransform.parent as RectTransform : null;
    }

    private void OnEnable()
    {
        // A re-enabled hand (e.g. after the camera transition between wave/inter-wave) always starts back
        // in plain cursor-following mode; CPIManager re-enters world-offset mode itself if a drag is still
        // in progress.
        EndWorldOffsetFollow();

        // Runs before this frame's first render, so a hand that was hidden appears already under the cursor
        // instead of gliding in from wherever (and however fast) it was last moving.
        SnapToCursor();

        // The Animator (on a child) may not be initialized yet at this point, and it restarts in its
        // default idle state after being disabled, so its parameters are re-synced on the first Update.
        _animatorNeedsSync = true;
    }

    private void SnapToCursor()
    {
        if (_rectTransform == null || _parentRectTransform == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRectTransform, Input.mousePosition, _canvasCamera, out var localPoint))
            return;

        _targetPoint = localPoint;
        _rectTransform.anchoredPosition = localPoint;
        _followVelocity = Vector2.zero;
    }

    private void Update()
    {
        FollowCursor();

        if (_animatorNeedsSync)
        {
            _animatorNeedsSync = false;

            // Starts in idle; only if the button is already held does it go straight to the pressed pose.
            if (_animator != null)
            {
                ApplyLayerWeights();

                _animator.SetBool(PointerDownBool, Input.GetMouseButton(0));
                _animator.SetBool(PointerUpBool, false);
            }
        }

        if (Input.GetMouseButtonDown(0))
            SetPointerDown(true);
        else if (Input.GetMouseButtonUp(0))
            SetPointerDown(false);
    }

    /// <summary>Pressing plays and holds the PointerDown pose; releasing plays and holds PointerUp.</summary>
    private void SetPointerDown(bool down)
    {
        if (_animator == null) return;

        _animator.SetBool(PointerDownBool, down);
        _animator.SetBool(PointerUpBool, !down);
    }

    /// <summary>The local point FollowCursor eases (or, while world-offset following, snaps) toward: the
    /// cursor normally, or — while world-offset following is active — Target's projected position plus the
    /// captured offset. The cursor is never consulted in the latter case, so nothing about the mouse,
    /// including vertical movement, affects the element while it's following the planet.</summary>
    private bool TryGetTargetLocalPoint(out Vector2 localPoint)
    {
        if (_worldOffsetFollowActive && _worldFollowTarget != null && _worldFollowCamera != null)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRectTransform, _worldFollowCamera.WorldToScreenPoint(_worldFollowTarget.position), _canvasCamera, out var targetLocalPoint))
            {
                localPoint = targetLocalPoint + _worldFollowOffset;
                return true;
            }
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRectTransform, Input.mousePosition, _canvasCamera, out localPoint);
    }

    private void FollowCursor()
    {
        if (_rectTransform == null || _parentRectTransform == null) return;

        if (!TryGetTargetLocalPoint(out var localPoint))
            return;

        if (_worldOffsetFollowActive)
        {
            // Tracks the planet exactly, with no smoothing lag, while its own drag is already eased —
            // stacking this element's smoothing on top would make it visibly trail the planet. Velocity is
            // zeroed so EndWorldOffsetFollow's switch back to cursor-following eases in cleanly instead of
            // inheriting leftover momentum from before the drag.
            _targetPoint = localPoint;
            _rectTransform.anchoredPosition = localPoint;
            _followVelocity = Vector2.zero;
            return;
        }

        if (!_targetPoint.HasValue)
        {
            _targetPoint = localPoint;
            _rectTransform.anchoredPosition = localPoint;
            return;
        }

        if ((localPoint - _targetPoint.Value).sqrMagnitude >= _deadZone * _deadZone)
            _targetPoint = localPoint;

        _rectTransform.anchoredPosition = Vector2.SmoothDamp(
            _rectTransform.anchoredPosition, _targetPoint.Value, ref _followVelocity, _smoothTime, _maxSpeed, Time.unscaledDeltaTime);
    }
}
