using UnityEngine;

public class DistractionUI : MonoBehaviour
{
    [SerializeField] private GameObject[] _levels;

    public void Show(int level)
    {
        for (int i = 0; i < _levels.Length; i++)
        {
            _levels[i].SetActive(i == level - 1);
        }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
