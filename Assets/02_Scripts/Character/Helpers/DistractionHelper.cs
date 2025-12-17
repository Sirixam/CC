using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
//using System.Diagnostics;

public class DistractionHelper
{
    [Serializable]
    public class Data
    {
        [Serializable]
        public class Level
        {
            public int MinCounter;
            public float DistractionDuration;
            public float DistractionRotationDelay;
            public float LookSpeedMultiplier;
            public bool Rotate;
        }

        [Tag]
        public string DistractionTag = "Distraction";
        public Level[] Levels;
    }

    private Data _data;
    private DistractionUI _distractionUI;
    private FieldOfViewController _fovController;
    private LookHelper _lookHelper;
    private AnswerController _answerController;
    private readonly StudentAudio _audio;

    private int _counter;

    public bool IsDistracted { get; private set; }

    public DistractionHelper(Data data, DistractionUI distractionUI, FieldOfViewController fovController, LookHelper lookHelper, AnswerController answerController, StudentAudio audio)
    {
        _data = data;
        _distractionUI = distractionUI;
        _fovController = fovController;
        _lookHelper = lookHelper;
        _answerController = answerController;
        _audio = audio;
    }

    public async UniTask OnDistracted(Vector3 hitDirection)
    {
        if (IsDistracted) return;

        IsDistracted = true;
        _answerController.UnblockCheat();

        _counter++;

        Data.Level levelData = GetLevelData(out int level);

        Debug.Log("level : " + level);
        //audio depending on level

        if (level == 1)
        {
            _audio.StartGrumblingSoft();
        }
        else if (level == 2)
        {
            _audio.StartGrumblingMild();
        }
        else if (level == 3)
        {
            _audio.StartGrumblingStrong();
        }

        _distractionUI.Show(level);

        await UniTask.WaitForSeconds(levelData.DistractionRotationDelay);

        Vector2 lookDirection = new Vector2(hitDirection.x, hitDirection.z).normalized;
        if (levelData.Rotate)
        {
            _lookHelper.AddLookMultiplier(levelData.LookSpeedMultiplier);
            _lookHelper.SetLookInput(lookDirection);
        }
        _fovController.Show();

        await UniTask.WaitForSeconds(levelData.DistractionDuration - levelData.DistractionRotationDelay);

        IsDistracted = false;
        _answerController.BlockCheat();
        _fovController.Hide();
        _distractionUI.Hide();
        if (levelData.Rotate)
        {
            _lookHelper.RemoveLookMultiplier(levelData.LookSpeedMultiplier);
            _lookHelper.RestoreInitialLookDirection();
        }
    }

    private Data.Level GetLevelData(out int level)
    {
        level = -1;
        for (int i = 0; i < _data.Levels.Length; i++)
        {
            if (_counter >= _data.Levels[i].MinCounter)
            {
                level = i + 1;
            }
        }
        if (level == -1)
        {
            level = _data.Levels.Length;
        }
        return _data.Levels[level - 1];
    }
}
