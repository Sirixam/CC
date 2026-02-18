using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class DistractionHelper
{
    [Serializable]
    public class Data
    {
        [Serializable]
        public class Level
        {
            public int MinDistraction;
            public float DistractionDuration;
            public float DistractionRotationDelay;
            public float LookSpeedMultiplier;
            public bool Rotate;
            public bool ShowFOV;
        }

        public float DistractionReductionDelay;
        public float DistractionReductionSpeed;
        public Level[] Levels;
    }

    private Data _data;
    private DistractionUI _distractionUI;
    private FieldOfViewController _fovController;
    private LookHelper _lookHelper;
    private AnswerController _answerController;
    private StudentAudioHelper _audioHelper;
    private TestDefinition _testDefinition;

    private int _accumulatedDistraction;
    private CancellationTokenSource _cancellationTokenSource;

    public bool IsDistracted { get; private set; }

    public DistractionHelper(Data data, DistractionUI distractionUI, FieldOfViewController fovController, LookHelper lookHelper, AnswerController answerController, StudentAudioHelper audioHelper)
    {
        _data = data;
        _distractionUI = distractionUI;
        _fovController = fovController;
        _lookHelper = lookHelper;
        _answerController = answerController;
        _audioHelper = audioHelper;
    }

    public void InjectTestDefinition(TestDefinition testDefinition)
    {
        _testDefinition = testDefinition;
    }

    public async UniTask OnDistracted(Vector3 hitDirection)
    {
        if (IsDistracted) return; // TODO: Handle multiple simultaneous distractions.

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        _accumulatedDistraction++; // TODO: Increased based on distraction and other factors.
        Data.Level levelData = GetLevelData(out int level);
        if (levelData == null)
        {
            Debug.Log("Accumulated distraction is too low to be distracted.");
            return;
        }

        IsDistracted = true;
        _answerController.UnblockCheat();
        _audioHelper.OnDistracted(level);
        _distractionUI.Show(level);

        await UniTask.WaitForSeconds(levelData.DistractionRotationDelay);

        Vector2 lookDirection = new Vector2(hitDirection.x, hitDirection.z).normalized;
        if (levelData.Rotate)
        {
            _lookHelper.AddLookMultiplier(levelData.LookSpeedMultiplier);
            _lookHelper.SetLookInput(lookDirection);
        }
        if (levelData.ShowFOV)
        {
            _fovController.Show();
        }

        await UniTask.WaitForSeconds(levelData.DistractionDuration - levelData.DistractionRotationDelay);

        IsDistracted = false;
        _answerController.BlockCheat();
        _distractionUI.Hide();
        if (levelData.ShowFOV)
        {
            _fovController.Hide();
        }
        if (levelData.Rotate)
        {
            _lookHelper.RemoveLookMultiplier(levelData.LookSpeedMultiplier);
            _lookHelper.RestoreInitialLookDirection();
        }

        await ReduceDistractionLevelOverTime(_cancellationTokenSource.Token);
    }

    public async UniTask ReduceDistractionLevelOverTime(CancellationToken cancellationToken)
    {
        if (_accumulatedDistraction == 0) return;

        await UniTask.WaitForSeconds(_data.DistractionReductionDelay, cancellationToken: cancellationToken);
        float toReduce = 0f;
        GetLevelData(out int lastLevel);
        while (_accumulatedDistraction > 0 && !cancellationToken.IsCancellationRequested)
        {
            await UniTask.Yield();
            toReduce += _data.DistractionReductionSpeed * Time.deltaTime;
            if (toReduce < 1) continue;

            int toReduceInt = Mathf.FloorToInt(toReduce);

            _accumulatedDistraction = Mathf.Max(0, _accumulatedDistraction - toReduceInt);
            toReduce -= toReduceInt;

            GetLevelData(out int level);
            if (lastLevel != level)
            {
                // TODO: Show some feedback of level reduction?
                Debug.Log($"Distraction level reduced to {level}");
            }

            if (level == 0) return;
        }
    }

    private Data.Level GetLevelData(out int level)
    {
#if UNITY_EDITOR
        if (_testDefinition != null && _testDefinition.ForcedDistractionLevel > 0)
        {
            level = _testDefinition.ForcedDistractionLevel;
            if (_data.Levels.Length >= level)
            {
                return _data.Levels[level - 1];
            }

            Debug.LogWarning($"Forced distraction level {level} is out of bounds of defined levels.");
        }
#endif
        level = -1;
        for (int i = 0; i < _data.Levels.Length; i++)
        {
            if (_accumulatedDistraction >= _data.Levels[i].MinDistraction)
            {
                level = i + 1;
            }
        }
        if (level == -1)
        {
            level = 0;
            return null;
        }
        return _data.Levels[level - 1];
    }
}
