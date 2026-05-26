using System;
using UnityEngine;

public class HandSlapMotion : MonoBehaviour
{
    [SerializeField] private Transform _handRoot;
    [SerializeField] private HandOpenController _openController;
    [SerializeField] private Vector3 _slapOffset = new Vector3(0.08f, 0.04f, -0.02f);
    [SerializeField] private Vector3 _slapRotation;
    [SerializeField] private float _slapDuration = 0.07f;
    [SerializeField] private float _returnDuration = 0.18f;

    private enum State { Idle, Slapping, Returning }

    private State _state;
    private Vector3 _basePos;
    private Quaternion _baseRot;
    private Quaternion _targetRot;
    private float _timer;
    private Action _onComplete;

    private void Update()
    {
        switch (_state)
        {
            case State.Slapping:
                _timer += Time.deltaTime;
                float slapT = Mathf.Clamp01(_timer / _slapDuration);
                _handRoot.localPosition = Vector3.Lerp(_basePos, _basePos + _slapOffset, slapT);
                _handRoot.localRotation = Quaternion.Slerp(_baseRot, _targetRot, slapT);
                if (slapT >= 1f) { _timer = 0f; _state = State.Returning; _openController?.Relax(); }
                break;

            case State.Returning:
                _timer += Time.deltaTime;
                float returnT = Mathf.Clamp01(_timer / _returnDuration);
                _handRoot.localPosition = Vector3.Lerp(_basePos + _slapOffset, _basePos, returnT);
                _handRoot.localRotation = Quaternion.Slerp(_targetRot, _baseRot, returnT);
                if (returnT >= 1f)
                {
                    _handRoot.localPosition = _basePos;
                    _handRoot.localRotation = _baseRot;
                    _state = State.Idle;
                    _onComplete?.Invoke();
                    _onComplete = null;
                }
                break;
        }
    }

    public void Play(Action onComplete)
    {
        _onComplete = onComplete;
        Play();
    }

    [Button("Play")]
    private void Play()
    {
        _basePos = _handRoot.localPosition;
        _baseRot = _handRoot.localRotation;
        _targetRot = _baseRot * Quaternion.Euler(_slapRotation);
        _timer = 0f;
        _state = State.Slapping;
        _openController?.Open();
    }
}
