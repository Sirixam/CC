using System;
using System.Collections.Generic;
using UnityEngine;

public class TeacherManager : MonoBehaviour
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
    }

    [SerializeField] private List<RouteData> _routesData;

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
}