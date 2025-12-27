using System.Collections.Generic;
using UnityEngine;

namespace _02_Scripts.Teacher
{
    public class VigilancePathsLvlManager : MonoBehaviour , IVigilancePathsLvlManager
    {
        [SerializeField] private List<VigilanceRoute> _vigilanceRoutes;
    }

    public interface IVigilancePathsLvlManager
    {
        
    }


    public class VigilanceRoute : MonoBehaviour, IVigilanceRoute
    {
        
    }

    public interface IVigilanceRoute
    {
        
    }

    public class VigilanceSpotGameObject : MonoBehaviour, IVigilanceSpotGameObject
    {
    
    }

    public interface IVigilanceSpotGameObject
    {
        
    }
}