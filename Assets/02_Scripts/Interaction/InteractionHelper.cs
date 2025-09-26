using System;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractionActor
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
        public ELog ScoreLogType = ELog.None;
    }

    private IInteractionActor _actor;
    private Data _data;
    private List<InteractionController> _interactions = new();
    private List<InteractionController> _activeInteractions = new();

    public InteractionController BestInteraction { get; private set; }

    public InteractionHelper(IInteractionActor actor, Data configurations)
    {
        _actor = actor;
        _data = configurations;
        _data.FacingScores.Sort((a, b) => a.MaxAngle.CompareTo(b.MaxAngle)); // Ensure ascending order
    }

    public void AddInteraction(InteractionController interaction)
    {
        interaction.OnDisableEvent += RemoveInteraction;
        _interactions.Add(interaction);
        UpdateBestInteraction();
    }

    public void RemoveInteraction(InteractionController interaction)
    {
        interaction.OnDisableEvent -= RemoveInteraction;
        _interactions.Remove(interaction);
        UpdateBestInteraction();
    }

    public void UpdateBestInteraction()
    {
        bool isCarrying = _activeInteractions.Exists(x => x.Type == EInteraction.PickUp);

        InteractionController bestInteraction = null;
        float bestScore = float.MinValue;

        foreach (InteractionController interaction in _interactions)
        {
            if (isCarrying && interaction.Type == EInteraction.PickUp)
                continue;

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

    public bool TryGetPickedUpInteraction(out InteractionController interaction)
    {
        interaction = _activeInteractions.Find(x => x.Type == EInteraction.PickUp);
        return interaction != null;
    }

    public bool TryStartInteraction(out InteractionController startedInteraction)
    {
        startedInteraction = BestInteraction;
        if (startedInteraction == null)
        {
            return false;
        }

        _activeInteractions.Add(startedInteraction);
        startedInteraction.OnStartInteraction();
        return true;
    }

    public bool TryStopInteraction(InteractionController stoppedInteraction)
    {
        if (!_activeInteractions.Remove(stoppedInteraction)) return false;
        stoppedInteraction.OnStopInteraction();
        return true;
    }
}
