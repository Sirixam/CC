using UnityEngine;

namespace AKGaming.Game
{
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowPreviewComponent : MonoBehaviour
    {
        [SerializeField] Transform _initialPoint;
        [Header("Configurations")]
        [SerializeField] private float _initialSpeed = 12f;
        [SerializeField] private float maxPredictionTime_ = 2f;
        [SerializeField] private int maxPointsCount_ = 100;
        [SerializeField] private float sphereCastRadius_ = 0.05f;
        [SerializeField] private LayerMask collisionMask_;
        [SerializeField] private float _lineAnimationSpeed = -1;
        [SerializeField] private float _particleFrecuency = 0.5f;
        //[SerializeField] private IdentifiableReference _particleIdRef = new(IdentifiableReference.EType.ID);

        private Vector3[] _points;
        private LineRenderer _lineRenderer;
        private float _timeStep;
        private float _timeToParticle;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
            UpdateConfigurationsCache();
        }

        /// Triggered by OnValueChanged attribute.
        private void UpdateConfigurationsCache()
        {
            if (!Application.isPlaying) return;

            _points = new Vector3[maxPointsCount_];
            _timeStep = maxPredictionTime_ / maxPointsCount_;
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
            Quaternion initialRotation = _initialPoint.rotation;
            Vector3 initialPosition = _initialPoint.position;
            Vector3 velocity = initialRotation * Vector3.forward * _initialSpeed;
            Vector3 previousPosition = initialPosition;

            for (int i = 0; i < maxPointsCount_; i++)
            {
                // InitialPosition + Velocity Delta + Gravity
                Vector3 nextPosition = initialPosition + _timeStep * i * velocity + 0.5f * Mathf.Pow(_timeStep * i, 2) * Physics.gravity;

                Vector3 heading = nextPosition - previousPosition;
                Vector3 direction = heading.normalized;
                float maxDistance = heading.magnitude;
                if (Physics.SphereCast(previousPosition, sphereCastRadius_, direction, out RaycastHit hit, maxDistance, collisionMask_))
                {
                    nextPosition = previousPosition + direction * (hit.point - previousPosition).magnitude;
                    _points[i] = nextPosition;

                    // If collision detected, end the line.
                    _lineRenderer.positionCount = i + 1;
                    _lineRenderer.SetPositions(_points);

                    lastPoint = previousPosition; // hit.point;
                    return;
                }

                _points[i] = nextPosition;
                previousPosition = nextPosition;
            }

            lastPoint = previousPosition; // [AKP] PreviousPosition is actually the last position here.
            _lineRenderer.positionCount = maxPointsCount_;
            _lineRenderer.SetPositions(_points);
        }
    }
}