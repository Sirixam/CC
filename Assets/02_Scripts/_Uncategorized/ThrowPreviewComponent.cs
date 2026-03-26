using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ThrowPreviewComponent : MonoBehaviour
{
    [SerializeField] Transform _initialPoint;
    [Header("Configurations")]
    [SerializeField] private float _maxPredictionTime = 2f;
    [SerializeField] private int _maxPointsCount = 100;
    [SerializeField] private float _sphereCastRadius = 0.01f;
    [SerializeField] private float _lineAnimationSpeed = -1;
    [SerializeField] private float _particleFrecuency = 0.5f;
    [SerializeField] private int _maxBounces = 1;
    [SerializeField] private float _bounciness = 0.6f;

    private Vector3[] _points;
    private LineRenderer _lineRenderer;
    private float _timeStep;
    private float _timeToParticle;
    private ChairHelper _chairHelper;
    private ThrowHelper.Data _throwData;
    public bool IsSitting => _chairHelper.IsSitting;
    private LayerMask _collisionMask;


    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
        UpdateConfigurationsCache();
    }
    public void Initialize(ChairHelper chairHelper, ThrowHelper.Data throwData, int flyingLayer)
    {
        _chairHelper = chairHelper;
        _throwData = throwData;
        _collisionMask = GetFlyingCollisionMask(flyingLayer);
        Debug.Log($"FlyingLayer: {flyingLayer}, CollisionMask: {_collisionMask.value}, Layers: {LayerMaskToString(_collisionMask)}");
    }

    /// Triggered by OnValueChanged attribute.
    private void UpdateConfigurationsCache()
    {
        if (!Application.isPlaying) return;

        _points = new Vector3[_maxPointsCount];
        _timeStep = _maxPredictionTime / _maxPointsCount;
    }


    private void LateUpdate()
    {
        if (!_lineRenderer.enabled) return;

        RenderPredictedPath(out Vector3 lastPoint);

        _timeToParticle -= Time.deltaTime;
        if (_timeToParticle <= 0)
        {
            _timeToParticle = _particleFrecuency;
            //ParticlesService.ShowParticle(_particleIdRef, lastPoint, scale: 1);
        }

        _lineRenderer.material.mainTextureOffset += new Vector2(Time.deltaTime * _lineAnimationSpeed, 0f);
    }

    public void Show()
    {
        _lineRenderer.enabled = true;
    }

    public void Hide()
    {
        _lineRenderer.enabled = false;
    }

    private void RenderPredictedPath(out Vector3 lastPoint)
    {
        /*
        Quaternion initialRotation = _initialPoint.rotation;
        Vector3 initialPosition = _initialPoint.position;
        Vector3 velocity = initialRotation * Vector3.forward * _initialSpeed;
        */

        Vector3 initialPosition = _initialPoint.position;
        Vector3 forward = _initialPoint.forward;
        forward.y = 0;
        forward.Normalize();
        Quaternion pitchRotation = Quaternion.AngleAxis(-_throwData.PitchAngle, _initialPoint.right);
        Vector3 velocity = pitchRotation * forward * _throwData.Speed;

        if (_chairHelper.IsSitting)
        {
            // Offset start position forward slightly to avoid starting inside nearby colliders
            initialPosition -= velocity.normalized * 0.3f;
        }

        Vector3 previousPosition = initialPosition;

        int pointIndex = 0;
        bool stopBouncing = false;


        for (int bounce = 0; bounce <= _maxBounces; bounce++)
        {
            for (int i = 0; i < _maxPointsCount - pointIndex; i++)
            {
                //in case the velocity is really neglibile to draw the preview
                if (stopBouncing) break;

                // InitialPosition + Velocity Delta + Gravity
                Vector3 nextPosition = initialPosition + _timeStep * i * velocity + 0.5f * Mathf.Pow(_timeStep * i, 2) * Physics.gravity;

                Vector3 heading = nextPosition - previousPosition;
                Vector3 direction = heading.normalized;

                float maxDistance = heading.magnitude;

                if (Physics.SphereCast(previousPosition, _sphereCastRadius, direction, out RaycastHit hit, maxDistance, _collisionMask))
                {
                    Vector3 hitPoint = previousPosition + direction * hit.distance;
                    //store hit point
                    _points[pointIndex] = hitPoint;
                    pointIndex++;

                    //_bounciness is the fallback in case there's no physics material
                    float bounciness = hit.collider.sharedMaterial != null
                        ? hit.collider.sharedMaterial.bounciness
                        : _bounciness;
                    /*
                    // calculate the exact velocity vector at impact
                    float t = _timeStep * i;
                    Vector3 velocityAtImpact = velocity + Physics.gravity * t;

                    // Reflect with energy loss
                    velocity = Vector3.Reflect(velocityAtImpact, hit.normal) * bounciness;

                    if (velocity.magnitude < 0.1f)
                    {
                        // Ball doesn't have enough energy to bounce meaningfully
                        stopBouncing = true;
                        break; 
                    }

                    // new starting position for next trajectory
                    initialPosition = hitPoint;
                    previousPosition = hitPoint;

                    break;

                    */
                    if (hit.normal.y > 0.7f) // surface is horizontal (desk top or floor)
                    {
                        // do bounce — this is a useful, predictable bounce
                        float t = _timeStep * i;
                        Vector3 velocityAtImpact = velocity + Physics.gravity * t;
                        velocity = Vector3.Reflect(velocityAtImpact, hit.normal) * bounciness;

                        if (velocity.magnitude < 0.1f)
                        {
                            stopBouncing = true;
                            break;
                        }

                        initialPosition = hitPoint;
                        previousPosition = hitPoint;
                        break;
                    }
                    else
                    {
                        // hit a desk edge, wall, or chair side — just stop the line here
                        _points[pointIndex] = hitPoint;
                        pointIndex++;
                        stopBouncing = true;
                        break;
                    }
                }
                _points[pointIndex] = nextPosition;
                pointIndex++;
            }
        }
        lastPoint = previousPosition; // [AKP] PreviousPosition is actually the last position here.
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
    private string LayerMaskToString(LayerMask mask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
                result += LayerMask.LayerToName(i) + ", ";
        }
        return result;
    }
}
