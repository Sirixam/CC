using UnityEngine;

public class SeatedGroupInputUI : MonoBehaviour
{
    private PlayerController _player;

    // Setup has to be called manually after instantiating the prefab, because we need a reference to the player
    public void Setup(PlayerController player)
    {
        _player = player;
        _player.OnSittingComplete += Show;
        _player.OnStandingStarted += Hide;

        if (_player.IsInitialized)
        {
            FinishInitialization();
        }
        else
        {
            _player.OnFinishedInitialization += FinishInitialization;
        }
    }

    private void FinishInitialization()
    {
        _player.OnFinishedInitialization -= FinishInitialization;
        gameObject.SetActive(_player.IsSitting);
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnSittingComplete -= Show;
            _player.OnStandingStarted -= Hide;
            _player.OnFinishedInitialization -= FinishInitialization;
        }
    }

    private void Show() => gameObject.SetActive(true);
    private void Hide() => gameObject.SetActive(false);
}
