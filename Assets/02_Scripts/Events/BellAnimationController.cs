using UnityEngine;


public class BellAnimationController: MonoBehaviour {

    private RoundTimeHelper _roundTimeHelper;
    private RoundTimeUI _roundTimeUI;
    private TimeHelper _timeHelper;
    private Animator animator;
    //private GameManager _gameManager;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private RoundTimeUI roundTimeUI;

    private void Start()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();

        _roundTimeHelper = gameManager.GetRoundTimeHelper();
        _timeHelper = gameManager.GetTimeHelper();

        Debug.Log("Helper: " + _roundTimeHelper);

        _roundTimeHelper.OnRoundTimesUp += playBellAnimation;
        _timeHelper.OnTimesUp += playLongBellAnimation;
    }

    private void OnDestroy()
    {
        if (_roundTimeHelper != null)
            _roundTimeHelper.OnRoundTimesUp -= playBellAnimation;
        if (_timeHelper != null)
            _timeHelper.OnTimesUp -= playLongBellAnimation;
    }
    public void playBellAnimation()
    {
        animator = GetComponent<Animator>();
        animator.SetTrigger("bellRoundEnd");
    }

    public void playLongBellAnimation()
    {
        animator = GetComponent<Animator>();
        animator.SetTrigger("endGame");
    }
}
