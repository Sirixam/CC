using UnityEngine;

public class LobThrowPreviewComponent : MonoBehaviour
{
    [SerializeField] private Transform _initialPoint;
    [SerializeField] private TrajectoryPreviewType _trajectoryPreview = TrajectoryPreviewType.Undefined;
    [SerializeField] private bool _renderLanding = false;
    [SerializeField] private LineRenderer _trajectoryLineRenderer;
    [SerializeField] private LineRenderer _landingLineRenderer;
    [SerializeField] private int _maxPointsCount = 50;
    [SerializeField] private float _lineAnimationSpeed = -1f;
    [SerializeField] private float _sphereCastRadius = 0.05f;
    [SerializeField] private Color _defaultColor = Color.yellow;
    [SerializeField] private Color _hitPlayerColor = Color.green;
    [SerializeField] private LayerMask _playerMask;
    [SerializeField] private float _landingRadius = 0.5f;
    [SerializeField] private int _landingSegments = 24;

    private Vector3[] _points;
    private Vector3[] _circlePoints;
    private LobThrowHelper _lobThrowHelper;
    private ChairHelper _chairHelper;
    private LayerMask _collisionMask;
    private bool _visible;

    private void Awake()
    {
        if (_trajectoryLineRenderer != null)
            _trajectoryLineRenderer.enabled = false;
        if (_landingLineRenderer != null)
            _landingLineRenderer.enabled = false;
        _points = new Vector3[_maxPointsCount];
        _circlePoints = new Vector3[_landingSegments];
    }

    public void Initialize(ChairHelper chairHelper, LobThrowHelper lobThrowHelper, int flyingLayer)
    {
        _chairHelper = chairHelper;
        _lobThrowHelper = lobThrowHelper;
        _collisionMask = GetFlyingCollisionMask(flyingLayer);
    }

    public void Show()
    {
        _visible = true;
        if (_trajectoryLineRenderer != null)
            _trajectoryLineRenderer.enabled = _trajectoryPreview != TrajectoryPreviewType.Undefined;
        if (_landingLineRenderer != null)
            _landingLineRenderer.enabled = _renderLanding;
    }

    public void Hide()
    {
        _visible = false;
        if (_trajectoryLineRenderer != null)
            _trajectoryLineRenderer.enabled = false;
        if (_landingLineRenderer != null)
            _landingLineRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (!_visible) return;

        if (_trajectoryPreview == TrajectoryPreviewType.Trajectory)
        {
            RenderTrajectory();
            _trajectoryLineRenderer.material.mainTextureOffset += new Vector2(Time.deltaTime * _lineAnimationSpeed, 0f);
        }
        else if (_trajectoryPreview == TrajectoryPreviewType.Line)
        {
            RenderLine();
            _trajectoryLineRenderer.material.mainTextureOffset += new Vector2(Time.deltaTime * _lineAnimationSpeed, 0f);
        }

        if (_renderLanding)
        {
            RenderLanding();
            _landingLineRenderer.material.mainTextureOffset += new Vector2(Time.deltaTime * _lineAnimationSpeed, 0f);
        }
    }

    private Vector3 SimulateToLanding(out bool willHitPlayer)
    {
        Vector3 startPos = _initialPoint.position;
        Vector3 velocity = _lobThrowHelper.CalculateThrowVelocity();
        float totalTime = _lobThrowHelper.GetData().FlightDuration;
        float timeStep = totalTime / _maxPointsCount;

        Vector3 previousPosition = startPos;
        Vector3 landingPosition = startPos;
        willHitPlayer = false;

        for (int i = 0; i < _maxPointsCount; i++)
        {
            float t = timeStep * i;
            Vector3 nextPosition = startPos + velocity * t + 0.5f * Physics.gravity * t * t;

            Vector3 heading = nextPosition - previousPosition;
            float maxDistance = heading.magnitude;

            if (maxDistance > 0.001f && Physics.SphereCast(previousPosition, _sphereCastRadius, heading.normalized, out RaycastHit hit, maxDistance, _collisionMask))
            {
                landingPosition = previousPosition + heading.normalized * hit.distance;
                if (((1 << hit.collider.gameObject.layer) & _playerMask) != 0)
                    willHitPlayer = true;
                return landingPosition;
            }

            landingPosition = nextPosition;
            previousPosition = nextPosition;
        }

        return landingPosition;
    }

    private void RenderTrajectory()
    {
        Vector3 startPos = _initialPoint.position;
        Vector3 velocity = _lobThrowHelper.CalculateThrowVelocity();
        float totalTime = _lobThrowHelper.GetData().FlightDuration;
        float timeStep = totalTime / _maxPointsCount;

        Vector3 previousPosition = startPos;
        int pointIndex = 0;
        bool willHitPlayer = false;

        for (int i = 0; i < _maxPointsCount; i++)
        {
            float t = timeStep * i;
            Vector3 nextPosition = startPos + velocity * t + 0.5f * Physics.gravity * t * t;

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

        _trajectoryLineRenderer.loop = false;
        _trajectoryLineRenderer.startColor = willHitPlayer ? _hitPlayerColor : _defaultColor;
        _trajectoryLineRenderer.endColor = willHitPlayer ? _hitPlayerColor : _defaultColor;
        _trajectoryLineRenderer.positionCount = pointIndex;
        _trajectoryLineRenderer.SetPositions(_points);
    }

    private void RenderLine()
    {
        Vector3 landingPosition = SimulateToLanding(out bool willHitPlayer);

        Color color = willHitPlayer ? _hitPlayerColor : _defaultColor;
        _trajectoryLineRenderer.loop = false;
        _trajectoryLineRenderer.startColor = color;
        _trajectoryLineRenderer.endColor = color;
        _trajectoryLineRenderer.positionCount = 2;
        _trajectoryLineRenderer.SetPosition(0, _initialPoint.position);
        _trajectoryLineRenderer.SetPosition(1, landingPosition);
    }

    private void RenderLanding()
    {
        Vector3 landingPosition = SimulateToLanding(out bool willHitPlayer);

        Color color = willHitPlayer ? _hitPlayerColor : _defaultColor;
        _landingLineRenderer.startColor = color;
        _landingLineRenderer.endColor = color;
        _landingLineRenderer.loop = true;
        _landingLineRenderer.positionCount = _landingSegments;

        for (int i = 0; i < _landingSegments; i++)
        {
            float angle = 2f * Mathf.PI * i / _landingSegments;
            _circlePoints[i] = new Vector3(
                landingPosition.x + _landingRadius * Mathf.Cos(angle),
                landingPosition.y,
                landingPosition.z + _landingRadius * Mathf.Sin(angle)
            );
        }

        _landingLineRenderer.SetPositions(_circlePoints);
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
