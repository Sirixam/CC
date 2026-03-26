using UnityEngine;
using UnityEngine.UI;

public class RoundTimeUI : MonoBehaviour
{
    [SerializeField] private Image _fill; // the existing opacity overlay

    [Header("Phase Arcs")] [SerializeField]
    private Image _phase1Arc;

    [SerializeField] private Image _phase2Arc;
    [SerializeField] private Image _phase3Arc;

    [Header("Phase Colors")] [SerializeField]
    private Color _phase1Color = Color.cyan;

    [SerializeField] private Color _phase2Color = Color.green;
    [SerializeField] private Color _phase3Color = Color.blue;
    
    [Header("Dividers")]
    [SerializeField] private RectTransform _divider1;
    [SerializeField] private RectTransform _divider2;

    private float _maxRoundTimeInSeconds;
    private float _phase1Duration;
    private float _phase2Duration;
    private float _phase3Duration;

    public void Setup(float seconds, float phase1Duration, float phase2Duration, float phase3Duration)
    {
        _fill.fillAmount = 0;
        _maxRoundTimeInSeconds = seconds;

        _phase1Duration = phase1Duration;
        _phase2Duration = phase2Duration;
        _phase3Duration = phase3Duration;

        BuildPhaseArcs();
    }

    private void BuildPhaseArcs()
    {
        float total = _phase1Duration + _phase2Duration + _phase3Duration;

        SetupArc(_phase1Arc, _phase1Duration / total, 0f, _phase1Color);

        float combinedDuration = _phase2Duration + _phase3Duration;
        SetupArc(_phase2Arc, combinedDuration / total, _phase1Duration / total, _phase2Color);

        _phase3Arc.gameObject.SetActive(false);
        
        BuildDividers(_phase1Duration + _phase2Duration + _phase3Duration);

    }

    private void SetupArc(Image arc, float fraction, float startFraction, Color color)
    {
        arc.type = Image.Type.Filled;
        arc.fillMethod = Image.FillMethod.Radial360;
        arc.fillClockwise = true;
        arc.fillOrigin = (int)Image.Origin360.Top;
        arc.fillAmount = fraction;
        arc.color = color;
        arc.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -startFraction * 360f);
    }

    public void SetRoundRemainingTime(float seconds)
    {
        float percent = Mathf.Clamp01((_maxRoundTimeInSeconds - seconds) / _maxRoundTimeInSeconds);
        _fill.fillAmount = percent;
    }

    public void ResetFill()
    {
        _fill.fillAmount = 0f;
    }

    private void BuildDividers(float total)
    {
        float angle1 = 0f; // start of Phase1 = top
        float angle2 = (_phase1Duration / total) * 360f;
        float angle3 = ((_phase1Duration + _phase2Duration) / total) * 360f;

        _divider1.localRotation = Quaternion.Euler(0f, 0f, -angle1);
        _divider2.localRotation = Quaternion.Euler(0f, 0f, -angle2);
    }
    
}

