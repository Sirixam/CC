using UnityEngine;

namespace AKGaming.Game
{
    public class HandWritingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _handRoot; // Wrist or Hand transform

        [Header("Motion Area")]
        [SerializeField] private Vector2 _amplitude = new Vector2(0.01f, 0.005f);
        [SerializeField] private float _speed = 2f;

        [Header("Rotation")]
        [SerializeField] private Vector3 _rotationAmplitude = new Vector3(5f, 2f, 3f);

        [Header("Noise")]
        [SerializeField] private float _noiseAmount = 0.002f;
        [SerializeField] private float _noiseSpeed = 6f;

        private Vector3 _initialPos;
        private Quaternion _initialRot;

        private float _time;

        private void Awake()
        {
            _initialPos = _handRoot.localPosition;
            _initialRot = _handRoot.localRotation;
        }

        private void Update()
        {
            _time += Time.deltaTime * _speed;

            ApplyPosition();
            ApplyRotation();
        }

        private void ApplyPosition()
        {
            float x = Mathf.Sin(_time) * _amplitude.x;
            float z = Mathf.Cos(_time * 0.7f) * _amplitude.y;

            // Add subtle noise
            float noiseX = (Mathf.PerlinNoise(_time * _noiseSpeed, 0f) - 0.5f) * _noiseAmount;
            float noiseZ = (Mathf.PerlinNoise(0f, _time * _noiseSpeed) - 0.5f) * _noiseAmount;

            Vector3 offset = new Vector3(x + noiseX, 0f, z + noiseZ);

            _handRoot.localPosition = _initialPos + offset;
        }

        private void ApplyRotation()
        {
            float tiltX = Mathf.Sin(_time * 1.2f) * _rotationAmplitude.x;
            float tiltY = Mathf.Cos(_time * 0.8f) * _rotationAmplitude.y;
            float tiltZ = Mathf.Sin(_time) * _rotationAmplitude.z;

            Quaternion rot = Quaternion.Euler(tiltX, tiltY, tiltZ);

            _handRoot.localRotation = _initialRot * rot;
        }
    }
}