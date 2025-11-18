using System;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractionActor : IActor
{
    Vector3 Position { get; }
    Vector3 Forward { get; }
}

public class InteractionHelper
{
    [Serializable]
    public class Data
    {
        [Serializable]
        public class AngleToScore
        {
            public float MaxAngle;
            public float Score;
        }

        public List<AngleToScore> FacingScores = new()
        {
            new AngleToScore() { MaxAngle = 60f, Score = 40f },
            new AngleToScore() { MaxAngle = 90f, Score = 15f },
        };

        public float BaseDistanceScore = 30f; // Score at distance 0
        public float DistanceScoreMultiplier = 40f;
        public bool InterpolateFacingScore = false;
        [Tag]
        public string[] InteractionTags;
        public ELog ScoreLogType = ELog.None;
    }

    private IInteractionActor _actor;
    private Data _data;
    private bool _isEnabled;
    private List<InteractionController> _interactions = new();
    private List<InteractionController> _activeInteractions = new();

    public InteractionController BestInteraction { get; private set; }

    public InteractionHelper(IInteractionActor actor, Data configurations, bool isEnabled)
    {
        _actor = actor;
        _data = configurations;
        _isEnabled = isEnabled;
        _data.FacingScores.Sort((a, b) => a.MaxAngle.CompareTo(b.MaxAngle)); // Ensure ascending order
    }

    public bool TryAddInteraction(Collider other, out InteractionController interaction)
    {
        if (HasAnyTag(other.transform, _data.InteractionTags))
        {
            interaction = other.GetComponentInParent<InteractionController>();
            if (interaction == null)
            {
                Debug.LogError("Interaction controller was not found in object tagged as interaction: " + other.transform.name);
                return false;
            }
            AddInteraction(interaction);
            return true;
        }
        interaction = default;
        return false;
    }

    public bool TryRemoveInteraction(Collider other, out InteractionController interaction)
    {
        if (HasAnyTag(other.transform, _data.InteractionTags))
        {
            interaction = other.GetComponentInParent<InteractionController>();
            if (interaction == null)
            {
                Debug.LogError("Interaction controller was not found in object tagged as interaction: " + other.transform.name);
                return false;
            }
            RemoveInteraction(interaction);
            return true;
        }
        interaction = default;
        return false;
    }

    public void EnableInteraction()
    {
        if (_isEnabled) return;
        _isEnabled = true;
        UpdateBestInteraction();
    }

    public void DisableInteraction()
    {
        if (!_isEnabled) return;
        _isEnabled = false;
        if (BestInteraction != null)
        {
            BestInteraction.DecreaseBestInteractionCount();
            BestInteraction = null;
        }
    }

    public void AddInteraction(InteractionController interaction)
    {
        interaction.OnDisableEvent += RemoveInteraction;
        interaction.OnDestroyEvent += OnDestroyInteraction;
        _interactions.Add(interaction);
        UpdateBestInteraction();
    }

    public void RemoveInteraction(InteractionController interaction)
    {
        interaction.OnDisableEvent -= RemoveInteraction;
        interaction.OnDestroyEvent -= OnDestroyInteraction;
        _interactions.Remove(interaction);
        UpdateBestInteraction();
    }

    public void UpdateBestInteraction()
    {
        if (!_isEnabled) return;

        bool isCarrying = _activeInteractions.Exists(x => x.Type == EInteraction.PickUp);

        InteractionController bestInteraction = null;
        float bestScore = float.MinValue;

        foreach (InteractionController interaction in _interactions)
        {
            if (!interaction.IsEnabled) continue;
            if (isCarrying && interaction.Type == EInteraction.PickUp) continue;
            if (!interaction.CanInteract(_actor.ID)) continue;

            float score = ComputeScore(interaction, isCarrying);
            if (score > bestScore)
            {
                bestScore = score;
                bestInteraction = score > 0 ? interaction : null;
            }
        }

        // Step 4: Apply
        if (BestInteraction != bestInteraction)
        {
            BestInteraction?.DecreaseBestInteractionCount();
            BestInteraction = bestInteraction;
            bestInteraction?.IncreaseBestInteractionCount();
        }
    }

    private float ComputeScore(InteractionController interaction, bool isCarrying)
    {
        // --- Distance factor ---
        float distance = Vector3.Distance(_actor.Position, interaction.Position);
        float distanceScore = _data.BaseDistanceScore - (distance * _data.DistanceScoreMultiplier);

        // --- Facing factor ---
        float yawToTarget = Vector3.SignedAngle(_actor.Forward, (interaction.Position - _actor.Position).normalized, Vector3.up);
        float absoluteYaw = Mathf.Abs(yawToTarget);
        float facingScore = _data.InterpolateFacingScore ? ComputeFacingFactorInterpolated(absoluteYaw) : ComputeFacingFactor(absoluteYaw);

        // --- Context factor ---
        float contextScore = isCarrying ? interaction.CarryingExtraScore : interaction.EmptyHandsExtraScore;

        float score = interaction.BaseScore + distanceScore + facingScore + contextScore;
        Logger.Log(_data.ScoreLogType, $"Score: {score}, interaction: {interaction.name}, yawToTarget: {yawToTarget}, baseScore: {interaction.BaseScore}, distanceScore: {distanceScore}, facingScore: {facingScore}, contextScore: {contextScore}");
        return score;
    }

    private float ComputeFacingFactor(float absoluteYaw)
    {
        foreach (var facingScoreData in _data.FacingScores)
        {
            if (absoluteYaw <= facingScoreData.MaxAngle)
            {
                return facingScoreData.Score;
            }
        }
        return 0;
    }

    private float ComputeFacingFactorInterpolated(float absoluteYaw)
    {
        for (int i = 0; i < _data.FacingScores.Count; i++)
        {
            var current = _data.FacingScores[i];
            if (absoluteYaw <= current.MaxAngle)
            {
                if (i == 0) return current.Score; // Nothing to interpolate

                // Interpolate between previous and current
                var prev = _data.FacingScores[i - 1];
                float t = Mathf.InverseLerp(prev.MaxAngle, current.MaxAngle, absoluteYaw);
                float score = Mathf.Lerp(prev.Score, current.Score, t);
                return score;
            }
        }
        return 0;
    }

    public bool TryGetPickedUpInteraction<TComponent>(out TComponent component)
    {
        InteractionController interaction = _activeInteractions.Find(x => x.Type == EInteraction.PickUp);
        if (interaction != null)
        {
            component = interaction.GetComponent<TComponent>();
            return component != null;
        }
        component = default;
        return false;
    }

    public bool TryGetPickedUpInteraction(out InteractionController interaction)
    {
        interaction = _activeInteractions.Find(x => x.Type == EInteraction.PickUp);
        return interaction != null;
    }

    public bool TryGetStaticInteraction(out InteractionController interaction)
    {
        interaction = _activeInteractions.Find(x => x.Type == EInteraction.Static);
        return interaction != null;
    }

    public void StartInteraction(InteractionController interaction)
    {
        _activeInteractions.Add(interaction);
        interaction.OnDestroyEvent += OnDestroyInteraction;
        interaction.OnStartInteraction();
    }

    public bool TryStopInteraction(InteractionController interaction)
    {
        if (!_activeInteractions.Remove(interaction)) return false;
        interaction.OnDestroyEvent -= OnDestroyInteraction;
        interaction.OnStopInteraction();
        return true;
    }

    private bool HasAnyTag(Transform target, string[] tags)
    {
        foreach (var tag in tags)
        {
            if (target.CompareTag(tag)) return true;
        }
        return false;
    }

    private void OnDestroyInteraction(InteractionController interaction)
    {
        interaction.OnDestroyEvent -= OnDestroyInteraction;
        TryStopInteraction(interaction);
        RemoveInteraction(interaction);
    }
}
