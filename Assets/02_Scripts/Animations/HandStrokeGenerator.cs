using UnityEngine;

namespace AKGaming.Game
{
    public class HandStrokeGenerator : MonoBehaviour
    {
        public enum StrokeType
        {
            Line,
            Wave,
            Loop
        }

        [Header("Stroke")]
        [SerializeField] private StrokeType _type = StrokeType.Wave;

        [SerializeField] private float _speed = 0.05f;
        [SerializeField] private float _scale = 0.01f;

        [Header("Wave")]
        [SerializeField] private float _waveAmplitude = 0.5f;
        [SerializeField] private float _waveFrequency = 2f;

        [Header("Loop")]
        [SerializeField] private float _loopRadius = 0.5f;

        private float _t;
        private Vector2 _current;

        public Vector2 Current => _current;

        private void Update()
        {
            _t += Time.deltaTime * _speed;

            switch (_type)
            {
                case StrokeType.Line:
                    GenerateLine();
                    break;

                case StrokeType.Wave:
                    GenerateWave();
                    break;

                case StrokeType.Loop:
                    GenerateLoop();
                    break;
            }
        }

        private void GenerateLine()
        {
            // Constant forward motion (like writing a word)
            _current = new Vector2(_t, 0f);
        }

        private void GenerateWave()
        {
            // Like handwriting oscillation
            float x = _t;
            float y = Mathf.Sin(_t * _waveFrequency) * _waveAmplitude;

            _current = new Vector2(x, y);
        }

        private void GenerateLoop()
        {
            // Circular / cursive loops
            float x = Mathf.Cos(_t) * _loopRadius;
            float y = Mathf.Sin(_t) * _loopRadius;

            // Move forward while looping
            x += _t * 0.5f;

            _current = new Vector2(x, y);
        }

        public Vector3 GetOffset3D()
        {
            return new Vector3(_current.x, 0f, _current.y) * _scale;
        }

        public void ResetStroke()
        {
            _t = 0f;
        }
    }
}