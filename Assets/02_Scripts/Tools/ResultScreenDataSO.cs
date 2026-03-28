using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02_Scripts.Tools
{
    [CreateAssetMenu(fileName = "DEF_ResultScreenData", menuName = "Definitions/Result Screen Data")]
    public class ResultScreenDataSO : ScriptableObject
    {
        public string title;
        public List<AvgScoreText> SubtitleScoreTexts;
        
        public string GetSubtitle(float score)
        {
            foreach (var s in SubtitleScoreTexts)
            {
                if (score >= s.min && score <= s.max)
                {
                    return s.text;
                }
            }
            return "No text matches score";
        }
        
        [Serializable]
        public class AvgScoreText
        {
            public float min;
            public float max;
            public string text;
        }
    }
}