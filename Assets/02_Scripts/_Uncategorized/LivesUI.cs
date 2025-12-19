using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    [SerializeField] private Image[] _lives;
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _emptyColor = Color.black;

    public void SetLives(int value)
    {
        for (int i = 0; i < _lives.Length; i++)
        {
            if (i < value)
            {
                _lives[i].color = _defaultColor;
            }
            else
            {
                _lives[i].color = _emptyColor;
            }
        }
    }
}
