using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlaneThrowPreviewComponent : MonoBehaviour
{
    [SerializeField] private Transform _initialPoint;
    [SerializeField] private int _maxPointsCount = 50;
    [SerializeField] private float _lineAnimationSpeed = -1f;
    [SerializeField] private float _sphereCastRadius = 0.05f;
    [SerializeField] private float _maxDistance = 30f;
    [SerializeField] private Color _defaultColor = Color.yellow;
    [SerializeField] private LayerMask _playerMask;

    private LineRenderer _lineRenderer;
    private PlaneThrowHelper _planeThrowHelper;
    private ChairHelper _chairHelper;
    private LayerMask _collisionMask;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
    }

    public void Initialize(ChairHelper chairHelper, PlaneThrowHelper planeThrowHelper, int flyingLayer)
    {
        _chairHelper = chairHelper;
        _planeThrowHelper = planeThrowHelper;
        _collisionMask = GetFlyingCollisionMask(flyingLayer);
    }

    public void Show() => _lineRenderer.enabled = true;
    public void Hide() => _lineRenderer.enabled = false;

    private void LateUpdate()
    {
        if (!_lineRenderer.enabled) return;
        RenderPath();
        _lineRenderer.material.mainTextureOffset += new Vector2(Time.deltaTime * _lineAnimationSpeed, 0f);
    }

    private void RenderPath()
    {
        Vector3 startPos = _initialPoint.position;        
        Vector3 endPos;
        _planeThrowHelper.CalculateThrowVelocity(out Vector3 direction);
        if (Physics.SphereCast(startPos, _sphereCastRadius, direction, out RaycastHit hit, _maxDistance, _collisionMask))
        {
            endPos = startPos + direction * hit.distance;
        }
        else
        {
            endPos = startPos + direction * _maxDistance;
        }

        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
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