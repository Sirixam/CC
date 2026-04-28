using System;
using UnityEngine;

public interface IItemController
{
    string LastOwnerID { get; }
    bool IsConfiscated { get; }
    bool CanConfiscate();
    void Confiscate(Transform point, Action onFreed);
    void Destroy();
}
