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

    private float _maxRoundTimeInSeconds;
    private float _phase1Duration;
    private float _phase2Duration;
    private float _phase3Duration;

    public void Setup(float seconds, GlobalDefinition globalDef)
    {
        _fill.fillAmount = 0;
        _maxRoundTimeInSeconds = seconds;

        // Resolve phase durations from GlobalDefinition (using midpoint of each range)
        _phase1Duration = (globalDef.PreAnsweringDelay.x + globalDef.PreAnsweringDelay.y) / 2f;
        _phase2Duration = (globalDef.AnsweringDuration.x + globalDef.AnsweringDuration.y) / 2f;
        _phase3Duration = (globalDef.PostAnsweringDelay.x + globalDef.PostAnsweringDelay.y) / 2f;

        BuildPhaseArcs();
    }

    private void BuildPhaseArcs()
    {
        float total = _phase1Duration + _phase2Duration + _phase3Duration;

        SetupArc(_phase1Arc, _phase1Duration / total, 0f, _phase1Color);
        SetupArc(_phase2Arc, _phase2Duration / total, _phase1Duration / total, _phase2Color);
        SetupArc(_phase3Arc, _phase3Duration / total, (_phase1Duration + _phase2Duration) / total, _phase3Color);
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

}

