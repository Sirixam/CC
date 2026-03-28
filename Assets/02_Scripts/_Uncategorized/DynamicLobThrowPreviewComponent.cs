using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DynamicLobThrowPreviewComponent : MonoBehaviour
{
    [SerializeField] private Transform _initialPoint;
    [SerializeField] private int _maxPointsCount = 50;
    [SerializeField] private float _lineAnimationSpeed = -1f;
    [SerializeField] private float _sphereCastRadius = 0.05f;
    [SerializeField] private Color _defaultColor = Color.yellow;
    [SerializeField] private Color _hitPlayerColor = Color.green;
    [SerializeField] private LayerMask _playerMask;

    private Vector3[] _points;
    private LineRenderer _lineRenderer;
    private DynamicLobThrowHelper _dynamicLobThrowHelper;
    private ChairHelper _chairHelper;
    private LayerMask _collisionMask;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
        _points = new Vector3[_maxPointsCount];
    }

    public void Initialize(ChairHelper chairHelper, DynamicLobThrowHelper helper, int flyingLayer)
    {
        _chairHelper = chairHelper;
        _dynamicLobThrowHelper = helper;
        _collisionMask = GetFlyingCollisionMask(flyingLayer);
    }

    public void Show() => _lineRenderer.enabled = true;
    public void Hide() => _lineRenderer.enabled = false;

    private void LateUpdate()
    {
        if (!_lineRenderer.enabled) return;
        RenderLobPath();
        _lineRenderer.material.mainTextureOffset += new Vector2(Time.deltaTime * _lineAnimationSpeed, 0f);
    }

    private void RenderLobPath()
    {
        Vector3 startPos = _initialPoint.position;
        Vector3 forward = _initialPoint.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 velocity = _dynamicLobThrowHelper.CalculateLobVelocity(forward);
        float totalTime = _dynamicLobThrowHelper.GetFlightDuration();
        float timeStep = totalTime / _maxPointsCount;
        Vector3 effectiveGravity = _dynamicLobThrowHelper.GetEffectiveGravity();

        Vector3 previousPosition = startPos;
        int pointIndex = 0;
        bool willHitPlayer = false;

        for (int i = 0; i < _maxPointsCount; i++)
        {
            float t = timeStep * i;
            Vector3 nextPosition = startPos + velocity * t + 0.5f * effectiveGravity * t * t;

            Vector3 heading = nextPosition - previousPosition;
            float maxDistance = heading.magnitude;

            if (maxDistance > 0.001f && Physics.SphereCast(previousPosition, _sphereCastRadius, heading.normalized, out RaycastHit hit, maxDistance, _collisionMask))
            {
                _points[pointIndex] = previousPosition + heading.normalized * hit.distance;
                pointIndex++;

                if (((1 << hit.collider.gameObject.layer) & _playerMask) != 0)
                    willHitPlayer = true;

                break;
            }

            _points[pointIndex] = nextPosition;
            pointIndex++;
            previousPosition = nextPosition;
        }

        _lineRenderer.startColor = willHitPlayer ? _hitPlayerColor : _defaultColor;
        _lineRenderer.endColor = willHitPlayer ? _hitPlayerColor : _defaultColor;
        _lineRenderer.positionCount = pointIndex;
        _lineRenderer.SetPositions(_points);
    }

    private LayerMask GetFlyingCollisionMask(int flyingLayer)
    {
        int mask = 0;
        for (int i = 0; i < 32; i++)
        {
            if (!Physics.GetIgnoreLayerCollision(flyingLayer, i))
                mask |= (1 << i);
        }
        return mask;
    }

}