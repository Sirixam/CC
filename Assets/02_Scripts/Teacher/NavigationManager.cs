using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NavigationManager : MonoBehaviour
{
    public enum ERoute
    {
        Undefined,
        Sequence,
    }

    [Serializable]
    public class RouteData
    {
        public ERoute Type;
        public WaypointData[] Waitpoints;
        public bool ShowOnGizmos;
    }

    [Serializable]
    public class WaypointData
    {
        public Transform Point;
        public float WaitTime;
    }

    [SerializeField] private List<RouteData> _routesData;
    [SerializeField] private bool _allowGizmos;

    public WaypointData[] GetRandomRoute()
    {
        int index = UnityEngine.Random.Range(0, _routesData.Count);
        return GetRouteAt(index);
    }

    public WaypointData[] GetRandomRouteNoRepeat(ref int lastRouteIndex)
    {
        int index = UnityEngine.Random.Range(0, _routesData.Count);
        if (_routesData.Count > 1)
        {
            while (index == lastRouteIndex)
            {
                index = UnityEngine.Random.Range(0, _routesData.Count);
            }
        }
        lastRouteIndex = index;
        return GetRouteAt(index);
    }

    private WaypointData[] GetRouteAt(int index)
    {
        RouteData routeData = _routesData[index];
        if (routeData.Type == ERoute.Sequence)
        {
            return routeData.Waitpoints;
        }

        Debug.LogError("Type is not being handled: " + routeData.Type);
        return new WaypointData[0];
    }

    private void OnDrawGizmos()
    {
        if (!_allowGizmos) return;

        Gizmos.color = Color.red;
        foreach (var routeData in _routesData)
        {
            if (!routeData.ShowOnGizmos) continue;

            foreach (var stepData in routeData.Waitpoints)
            {
                Gizmos.DrawSphere(stepData.Point.position, 0.5f);
            }
        }
    }

#if UNITY_EDITOR
    [Button("Disable All Gizmos Flags")]
    private void DisableAllGizmosFlags()
    {
        foreach (var routeData in _routesData)
        {
            routeData.ShowOnGizmos = false;
        }
        EditorUtility.SetDirty(this);
    }
#endif
}