using PrimeTween;
using UnityEngine;
using _02_Scripts.Utils;

public class GradeBoostItem : MonoBehaviour
{
    [Header("Grade Boost")]
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField, Range(0f, 1f), Tooltip("Correctness added to the player's worst answer on pickup.")]
    private float _gradeBoost = 0.5f;

    [Header("Lifetime")]
    [SerializeField] private float _lifetime = 10f;
    [SerializeField, Tooltip("Seconds before destruction when the warning animation starts.")]
    private float _warningTime = 3f;

    [Header("Float")]
    [SerializeField] private float _floatAmplitude = 0.1f;
    [SerializeField] private float _floatDuration = 1f;

    [Header("Warning")]
    [SerializeField] private float _warningPulseScale = 1.25f;
    [SerializeField] private float _warningPulseDuration = 0.2f;

    private Tween _floatTween;
    private Sequence _warningTween;
    private float _timer;
    private bool _collected;
    private bool _warningStarted;
    private Vector3 _restLocalPosition;

    private void Start()
    {
        _restLocalPosition = transform.localPosition;
        StartFloat();
    }

    private void Update()
    {
        if (_collected) return;

        _timer += Time.deltaTime;

        if (!_warningStarted && _timer >= _lifetime - _warningTime)
        {
            _warningStarted = true;
            StartWarning();
        }

        if (_timer >= _lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag(_globalDefinition.PlayerTag)) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        Collect(player);
    }

    private void Collect(PlayerController player)
    {
        _collected = true;
        ApplyGradeBoost(player);
        Destroy(gameObject);
    }

    private void ApplyGradeBoost(PlayerController player)
    {
        AnswerSheet sheet = player.GetAnswerSheet();
        if (sheet == null || sheet.Answers == null) return;

        // Find the answer with the lowest correctness that isn't already full
        Answer worstAnswer = null;
        float worstCorrectness = float.MaxValue;

        foreach (var answer in sheet.Answers)
        {
            if (answer.Correctness < 1f && answer.Correctness < worstCorrectness)
            {
                worstCorrectness = answer.Correctness;
                worstAnswer = answer;
            }
        }

        if (worstAnswer == null) return;

        float boosted = Mathf.Clamp01(worstAnswer.Correctness + _gradeBoost);
        sheet.SetCorrectness(worstAnswer.ID, boosted);
    }

    private void StartFloat()
    {
        _floatTween = Tween.LocalPositionY(
            transform,
            startValue: _restLocalPosition.y - _floatAmplitude,
            endValue:   _restLocalPosition.y + _floatAmplitude,
            duration:   _floatDuration,
            ease:       Ease.InOutSine,
            cycles:     -1,
            cycleMode:  CycleMode.Yoyo);
    }

    private void StartWarning()
    {
        _warningTween = Sequence.Create(cycles: -1)
            .Chain(Tween.Scale(transform, Vector3.one * _warningPulseScale, _warningPulseDuration, Ease.OutQuad))
            .Chain(Tween.Scale(transform, Vector3.one, _warningPulseDuration, Ease.InQuad));
    }
}
