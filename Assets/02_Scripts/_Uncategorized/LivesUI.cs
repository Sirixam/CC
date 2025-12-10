using TMPro;
using UnityEngine;

public class LivesUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _livesText;

    public void SetLives(int value)
    {
        _livesText.text = $"x{value}";
    }
}
