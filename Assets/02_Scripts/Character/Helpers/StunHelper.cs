using System;
using UnityEngine;

public interface IStunView
{
    void OnStartStun(bool isSoftStun);
    void OnStopStun();
}

public class StunHelper
{
    [Serializable]
    public class Data
    {
        public float HardStunDuration = 1f;
        public float SoftStunDuration = 0.5f;
    }

    private Data _data;
    private IStunView _ownerView;
    private float _stunTimer;
    public bool IsStunned { get; private set; }

    public StunHelper(Data data, IStunView ownerView)
    {
        _data = data;
        _ownerView = ownerView;
    }

    public void StartStun(bool isSoftStun)
    {
        _stunTimer = isSoftStun ? _data.SoftStunDuration : _data.HardStunDuration;
        IsStunned = true;
        _ownerView.OnStartStun(isSoftStun);
    }

    public void UpdateStun()
    {
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0)
        {
            IsStunned = false;
            _ownerView.OnStopStun();
        }
    }

    public void ForceStop()
    {
        IsStunned = false;
    }

}
