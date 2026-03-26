using System;
using UnityEngine;

public class HandProceduralAnimator : MonoBehaviour
{
    private interface IFinger
    {
        string Name { get; }
        float Curl { get; set; }

        void Initialize();
        void Apply();
    }

    [Serializable]
    public class ThumbFinger : IFinger
    {
        public Transform[] Bones;

        [Header("Curl")]
        public float[] CurlAngles;
        public Vector3 CurlAxis;

        [Header("Opposition")]
        public float[] OpposeAngles;
        public Vector3 OpposeAxis;

        [Range(0f, 1f)] public float Curl;
        [Range(0f, 1f)] public float Oppose;

        private Quaternion[] _initialRotations;

        public void Initialize()
        {
            _initialRotations = new Quaternion[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
                _initialRotations[i] = Bones[i].localRotation;
        }

        // IFinger
        string IFinger.Name => "Thumb";
        float IFinger.Curl { get => Curl; set => Curl = value; }

        void IFinger.Apply()
        {
            int count = Mathf.Min(Bones.Length, CurlAngles.Length, OpposeAngles.Length);

            for (int i = 0; i < count; i++)
            {
                Quaternion curlRot = Quaternion.AngleAxis(CurlAngles[i] * Curl, CurlAxis);
                Quaternion opposeRot = Quaternion.AngleAxis(OpposeAngles[i] * Oppose, OpposeAxis);

                Bones[i].localRotation = _initialRotations[i] * opposeRot * curlRot;
            }
        }
    }

    [Serializable]
    public class Finger : IFinger
    {
        public string Name;

        [Tooltip("Ordered from base -> tip")]
        public Transform[] Bones;

        [Tooltip("Max bend per bone in degrees")]
        public float[] MaxAngles;

        [Tooltip("Axis in local space")]
        public Vector3 BendAxis;

        [Range(0f, 1f)]
        public float Curl;

        private Quaternion[] _initialRotations;

        // IFinger
        string IFinger.Name => Name;
        float IFinger.Curl { get => Curl; set => Curl = value; }

        public void Initialize()
        {
            _initialRotations = new Quaternion[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
                _initialRotations[i] = Bones[i].localRotation;
        }

        void IFinger.Apply()
        {
            int count = Mathf.Min(Bones.Length, MaxAngles.Length);

            for (int i = 0; i < count; i++)
            {
                float angle = MaxAngles[i] * Curl;
                Quaternion rot = Quaternion.AngleAxis(angle, BendAxis);

                Bones[i].localRotation = _initialRotations[i] * rot;
            }
        }
    }

    [Header("Fingers")]
    [SerializeField] private ThumbFinger _thumb;
    [SerializeField] private Finger _index;
    [SerializeField] private Finger _middle;
    [SerializeField] private Finger _ring;
    [SerializeField] private Finger _little;

    public ThumbFinger Thumb => _thumb;
    public Finger Index => _index;
    public Finger Middle => _middle;
    public Finger Ring => _ring;
    public Finger Little => _little;

    private IFinger[] _allFingers;

    private void Awake()
    {
        _allFingers = new IFinger[] { _thumb, _index, _middle, _ring, _little };
        foreach (var finger in _allFingers)
        {
            finger.Initialize();
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < _allFingers.Length; i++)
        {
            _allFingers[i]?.Apply();
        }
    }

    // --- API ---

    public void SetAllCurl(float value)
    {
        value = Mathf.Clamp01(value);

        foreach (var finger in _allFingers)
            finger.Curl = value;
    }

    public void SetFingerCurl(string fingerName, float value)
    {
        value = Mathf.Clamp01(value);

        foreach (var finger in _allFingers)
        {
            if (finger.Name == fingerName)
            {
                finger.Curl = value;
                return;
            }
        }
    }

    public void SetIndividualCurls(float thumb, float index, float middle, float ring, float little)
    {
        _thumb.Curl = Mathf.Clamp01(thumb);
        _index.Curl = Mathf.Clamp01(index);
        _middle.Curl = Mathf.Clamp01(middle);
        _ring.Curl = Mathf.Clamp01(ring);
        _little.Curl = Mathf.Clamp01(little);
    }
}