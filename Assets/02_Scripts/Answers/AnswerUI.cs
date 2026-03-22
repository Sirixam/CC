using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _numberText;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _notFilledState;
    [SerializeField] private GameObject _filledState;
    [SerializeField] private Image _fill;
    [SerializeField] private Image _correctnessColorTarget;
    [Header("COLORS")]
    [SerializeField] private Color _correctColor = Color.green;
    [SerializeField] private Color _incorrectColor = Color.red;
    [SerializeField] private Color _halfCorrectColor = Color.yellow;

    public string ID { get; private set; }

    public void SetID(string value)
    {
        ID = value;
    }

    public void SetIcon(Sprite icon, Color color)
    {
        _icon.sprite = icon;
        _icon.color = color;
    }

    public void SetNumber(int value, Color color)
    {
        if (_numberText != null)
        {
            _numberText.text = value.ToString();
            _numberText.color = color;
        }
    }

    public void SetState(bool isFilled)
    {
        if (_notFilledState != null)
        {
            _notFilledState.SetActive(!isFilled);
        }
        if (_filledState != null)
        {
            _filledState.SetActive(isFilled);
        }
    }

    public void SetCorrectness(float correctness)
    {
        if (_correctnessColorTarget != null)
        {
            _correctnessColorTarget.color = correctness == 0 ? _incorrectColor : correctness == 1 ? _correctColor : _halfCorrectColor;
        }
    }

    public void SetProgress(float percent)
    {
        if (_fill != null)
        {
            _fill.fillAmount = percent;
        }
    }
    
    public void SetFinalState(bool isFilled, float correctness)
    {
        // Base state (filled vs not filled)
        SetState(isFilled);

        if (!isFilled)
        {
            // NOT ANSWERED
            if (_correctnessColorTarget != null)
                _correctnessColorTarget.color = Color.gray;

            if (_icon != null)
                _icon.color = new Color(1f, 1f, 1f, 0.5f); // faded icon

            // slightly smaller
            transform.localScale = Vector3.one * 0.95f;

            return;
        }

        // Reset icon visibility for answered states
        if (_icon != null)
            _icon.color = Color.white;

        // Apply correctness color (uses your existing logic)
        SetCorrectness(correctness);

        // subtle emphasis
        if (correctness < 1f)
        {
            // Partial / incorrect
            transform.localScale = Vector3.one * 1.05f;
        }
        else
        {
            // Correct 
            transform.localScale = Vector3.one;
        }
    }
}
