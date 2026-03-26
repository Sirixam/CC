using UnityEngine;

public class HandWritingLoopController : MonoBehaviour
{
    private enum State
    {
        Writing,
        Resetting
    }

    private enum ResetPhase
    {
        Lift,
        Return
    }

    [Header("References")]
    [SerializeField] private Transform _handRoot;
    [SerializeField] private HandStrokeGenerator _stroke;
    [SerializeField] private HandWritingMotion _writingMotion;

    [Header("Write Settings")]
    [SerializeField] private float _writeDuration = 2.5f;
    [SerializeField] private float _maxDistance = 0.08f;

    [Header("Reset Settings")]
    [SerializeField] private float _resetSpeed = 5f;

    [Header("Offsets")]
    [SerializeField] private float _liftHeight = 0.01f;
    [SerializeField] private Vector2 _returnInfluenceRange = new Vector2(0.1f, 0.5f);


    private State _state;
    private ResetPhase _resetPhase;
    private float _timer;

    private Vector3 _initialPos;
    private Quaternion _initialRot;
    private Vector3 _writeStartPos;
    private Vector3 _velocity; // for SmoothDamp


    private void Awake()
    {
        _initialPos = _handRoot.localPosition;
        _initialRot = _handRoot.localRotation;
        _writeStartPos = _initialPos;
    }

    private void OnEnable()
    {
        StartWriting();
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Writing:
                UpdateWriting();
                break;

            case State.Resetting:
                UpdateResetting();
                break;
        }
    }

    // -------------------------
    // Writing Phase
    // -------------------------
    private void UpdateWriting()
    {
        _timer += Time.deltaTime;

        Vector3 strokeOffset = _stroke.GetOffset3D();
        Vector3 motionOffset = _writingMotion != null ? _writingMotion.PositionOffset : Vector3.zero;
        Vector3 target = _writeStartPos + strokeOffset + motionOffset;

        _handRoot.localPosition = target;

        if (_writingMotion != null)
        {
            _handRoot.localRotation = _initialRot * _writingMotion.RotationOffset;
        }

        float distance = Mathf.Abs(strokeOffset.x);
        if (_timer >= _writeDuration || distance >= _maxDistance)
        {
            StartReset();
        }
    }

    private void StartWriting()
    {
        _state = State.Writing;
        _timer = 0f;

        _stroke.ResetStroke();
        _writeStartPos = _initialPos;

        if (_writingMotion != null)
        {
            _writingMotion.Reset();
            _writingMotion.enabled = true;
        }
    }

    // -------------------------
    // Reset Phase
    // -------------------------
    private void StartReset()
    {
        _state = State.Resetting;
        _resetPhase = ResetPhase.Lift;

        if (_writingMotion != null)
        {
            _writingMotion.enabled = false;
        }
    }

    private void UpdateResetting()
    {
        switch (_resetPhase)
        {
            case ResetPhase.Lift:
                UpdateLift();
                break;

            case ResetPhase.Return:
                UpdateReturn();
                break;
        }
    }

    private void UpdateLift()
    {
        Vector3 current = _handRoot.localPosition;

        // Target height (lift)
        float targetY = _initialPos.y + _liftHeight;

        float heightProgress = Mathf.InverseLerp(_initialPos.y, targetY, current.y);
        float returnInfluence = Mathf.SmoothStep(_returnInfluenceRange.x, _returnInfluenceRange.y, heightProgress);

        // Blend XZ slightly back toward start (not fully)
        Vector3 targetXZ = Vector3.Lerp(new Vector3(current.x, 0f, current.z), new Vector3(_initialPos.x, 0f, _initialPos.z), returnInfluence);

        Vector3 target = new Vector3(targetXZ.x, targetY, targetXZ.z);

        _handRoot.localPosition = Vector3.SmoothDamp(current, target, ref _velocity, 0.08f);

        // Check lift completion (only Y matters here)
        if (Mathf.Abs(_handRoot.localPosition.y - targetY) < 0.001f)
        {
            _resetPhase = ResetPhase.Return;
            _velocity = Vector3.zero;
        }
    }

    private void UpdateReturn()
    {
        _handRoot.localPosition = Vector3.SmoothDamp(_handRoot.localPosition, _initialPos, ref _velocity, 0.12f);

        if (Vector3.Distance(_handRoot.localPosition, _initialPos) < 0.001f)
        {
            // Final settle
            _handRoot.localPosition = _initialPos;

            StartWriting();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_handRoot == null) return;

        Transform parent = _handRoot.parent != null ? _handRoot.parent : _handRoot;
        Vector3 localPos = Application.isPlaying ? _initialPos : _handRoot.localPosition;

        Vector3 origin = parent.TransformPoint(localPos);
        Vector3 writeEnd = parent.TransformPoint(localPos + Vector3.right * _maxDistance);
        Vector3 liftTop = parent.TransformPoint(localPos + Vector3.up * _liftHeight);
        Vector3 corner = parent.TransformPoint(localPos + Vector3.right * _maxDistance + Vector3.up * _liftHeight);

        // Writing range (horizontal)
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f);
        Gizmos.DrawLine(origin, writeEnd);
        Gizmos.DrawWireSphere(writeEnd, 0.003f);

        // Lift height (vertical)
        Gizmos.color = new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawLine(origin, liftTop);
        Gizmos.DrawWireSphere(liftTop, 0.003f);

        // Boundary rectangle
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawLine(writeEnd, corner);
        Gizmos.DrawLine(liftTop, corner);

        // Rest position
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(origin, 0.005f);
    }
#endif
}