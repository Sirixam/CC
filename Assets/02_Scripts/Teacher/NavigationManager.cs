using System;
using System.Collections.Generic;
using UnityEngine;

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
        public Transform[] Points;
        public bool ShowOnGizmos;
    }

    [SerializeField] private List<RouteData> _routesData;
    [SerializeField] private bool _allowGizmos;

    public Transform[] GetRandomRoute()
    {
        int index = UnityEngine.Random.Range(0, _routesData.Count);
        return GetRouteAt(index);
    }

    public Transform[] GetRandomRouteNoRepeat(ref int lastRouteIndex)
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

    private Transform[] GetRouteAt(int index)
    {
        RouteData routeData = _routesData[index];
        if (routeData.Type == ERoute.Sequence)
        {
            return routeData.Points;
        }

        Debug.LogError("Type is not being handled: " + routeData.Type);
        return new Transform[0];
    }

    private void OnDrawGizmos()
    {
        if (!_allowGizmos) return;

        Gizmos.color = Color.red;
        foreach (var routeData in _routesData)
        {
            if (!routeData.ShowOnGizmos) continue;

            foreach (var point in routeData.Points)
            {
                Gizmos.DrawSphere(point.position, 0.5f);
            }
        }
    }
}