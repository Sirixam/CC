using UnityEngine;

namespace _02_Scripts.Tools
{
    [CreateAssetMenu(fileName = "DEF_PlayerAppearance", menuName = "Definitions/Player Appearance")]
    public class PlayerAppearanceSO : ScriptableObject
    {
        public Color HairColor;
        public Color ClothesColor;
    }
}