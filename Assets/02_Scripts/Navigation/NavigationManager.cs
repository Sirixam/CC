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
    public class IdentifiableRouteData : RouteData
    {
        public string Name;
    }

    [Serializable]
    public class RouteData
    {
        [ToggleButtons("INCLUDE", "IGNORE")]
        public bool Include;
        public ERoute Type;
        public WaypointData[] Waitpoints;
        public bool ShowOnGizmos;
    }

    [Serializable]
    public class WaypointData
    {
        public Transform Point;
        public float NextDelay;
        public EventDefinition ArriveEvent;
    }

    [SerializeField] private List<IdentifiableRouteData> _identifiableRoutesData;
    [SerializeField] private List<RouteData> _routesData;
    [SerializeField] private bool _allowGizmos;

    public WaypointData[] GetRoute(string routeName)
    {
        foreach (var identifiableRouteData in _identifiableRoutesData)
        {
            if (identifiableRouteData.Name == routeName)
            {
                return identifiableRouteData.Waitpoints;
            }
        }
        Debug.LogError("Route not found: " + routeName);
        return new WaypointData[0];
    }

    public WaypointData[] GetRandomRoute()
    {
        List<int> validIndices = GetValidIndices();
        if (validIndices.Count == 0)
        {
            return Array.Empty<WaypointData>();
        }

        int index = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
        //Debug.Log("GetRandomRoute, Index: " + index);
        return GetRouteAt(index);
    }

    public WaypointData[] GetRandomRouteNoRepeat(ref int lastRouteIndex)
    {
        List<int> validIndices = GetValidIndices();
        if (validIndices.Count == 0)
        {
            return Array.Empty<WaypointData>();
        }

        // Remove last route if we have alternatives
        if (validIndices.Count > 1)
        {
            validIndices.Remove(lastRouteIndex);
        }

        int index = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
        lastRouteIndex = index;
        //Debug.Log("GetRandomRouteNoRepeat, Index: " + index);
        return GetRouteAt(index);
    }

    private List<int> GetValidIndices()
    {
        List<int> validIndices = new();
        for (int i = 0; i < _routesData.Count; i++)
        {
            if (_routesData[i].Include)
            {
                validIndices.Add(i);
            }
        }
        //Debug.Log("GetValidIndices, Count: " + validIndices.Count);
        return validIndices;
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

    [Button("Include All Routes", ButtonColor.Green)]
    private void IncludeAllRoutes()
    {
        foreach (var routeData in _routesData)
        {
            routeData.Include = true;
        }
    }

    [Button("Exclude All Routes", ButtonColor.Red)]
    private void ExcludeAllRoutes()
    {
        foreach (var routeData in _routesData)
        {
            routeData.Include = false;
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