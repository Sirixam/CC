using PrimeTween;
using UnityEngine;
using _02_Scripts.Utils;

public class GradeBoostItem : MonoBehaviour
{
    [Header("Grade Boost")]
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private float _gradeBoost = 0.5f;

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

    [Header("Collect Particle")]
    [SerializeField] private ParticleSystem _collectParticle;

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
        PlayCollectParticle();
        Destroy(gameObject);
    }

    private void PlayCollectParticle()
    {
        if (_collectParticle == null) return;

        // Detach so it outlives the item, then auto-destroy when done
        _collectParticle.transform.SetParent(null, worldPositionStays: true);
        _collectParticle.Play();
        Destroy(_collectParticle.gameObject, _collectParticle.main.duration + _collectParticle.main.startLifetime.constantMax);
    }

    private void ApplyGradeBoost(PlayerController player)
    {
        AnswerSheet sheet = GameContext.AnswersManager.GetPlayerSheet(player.ID);
        if (sheet == null) return;

        sheet.AddGradeBoost(_gradeBoost);
    }

    private void StartFloat()
    {
        _floatTween = Tween.LocalPositionY(
            transform,
            startValue: _restLocalPosition.y - _floatAmplitude,
            endValue: _restLocalPosition.y + _floatAmplitude,
            duration: _floatDuration,
            ease: Ease.InOutSine,
            cycles: -1,
            cycleMode: CycleMode.Yoyo);
    }

    private void StartWarning()
    {
        _warningTween = Sequence.Create(cycles: -1)
            .Chain(Tween.Scale(transform, Vector3.one * _warningPulseScale, _warningPulseDuration, Ease.OutQuad))
            .Chain(Tween.Scale(transform, Vector3.one, _warningPulseDuration, Ease.InQuad));
    }
}
