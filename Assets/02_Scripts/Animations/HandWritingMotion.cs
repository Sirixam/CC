using UnityEngine;

public class HandWritingMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _handRoot; // Wrist or Hand transform
    [SerializeField] private HandStrokeGenerator _stroke;

    [Header("Motion Area")]
    [SerializeField] private Vector2 _amplitude = new Vector2(0.01f, 0.005f);
    [SerializeField] private float _speed = 2f;

    [Header("Rotation")]
    [SerializeField] private Vector3 _rotationAmplitude = new Vector3(5f, 2f, 3f);

    [Header("Noise")]
    [SerializeField] private float _noiseAmount = 0.002f;
    [SerializeField] private float _noiseSpeed = 6f;

    private float _time;

    public Vector3 PositionOffset { get; private set; }
    public Quaternion RotationOffset { get; private set; }

    private void Update()
    {
        _time += Time.deltaTime * _speed;

        ComputePositionOffset();
        ComputeRotationOffset();
    }

    private void ComputePositionOffset()
    {
        float x = Mathf.Sin(_time) * _amplitude.x;
        float z = Mathf.Cos(_time * 0.7f) * _amplitude.y;

        float noiseX = (Mathf.PerlinNoise(_time * _noiseSpeed, 0f) - 0.5f) * _noiseAmount;
        float noiseZ = (Mathf.PerlinNoise(0f, _time * _noiseSpeed) - 0.5f) * _noiseAmount;

        PositionOffset = new Vector3(x + noiseX, 0f, z + noiseZ);
    }

    private void ComputeRotationOffset()
    {
        float tiltX = Mathf.Sin(_time * 1.2f) * _rotationAmplitude.x;
        float tiltY = Mathf.Cos(_time * 0.8f) * _rotationAmplitude.y;
        float tiltZ = Mathf.Sin(_time) * _rotationAmplitude.z;

        RotationOffset = Quaternion.Euler(tiltX, tiltY, tiltZ);
    }

    public void Reset()
    {
        _time = 0;
    }
}