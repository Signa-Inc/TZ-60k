using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    [SerializeField] Battle battle;
    [SerializeField] Cannon cannon;
    [SerializeField] GameObject startScreen;
    [SerializeField] Hud hud;
    [SerializeField] ResultScreen resultScreen;
    [SerializeField] MetaScreen metaScreen;
    [SerializeField] Button startButton, retryButton, upgradeButton, fightButton;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        startButton.onClick.AddListener(StartBattle);
        retryButton.onClick.AddListener(StartBattle);
        fightButton.onClick.AddListener(StartBattle);
        upgradeButton.onClick.AddListener(OpenMeta);
        battle.Finished += ShowResult;
    }

    void Start()
    {
        Open(startScreen);
        cannon.ResetPosition();
        battle.ShowIdle();
    }

    public void StartBattle()
    {
        cannon.gameObject.SetActive(true);
        Open(hud.gameObject);
        Fx.Instance.ClearUi();
        battle.Begin();
    }

    void ShowResult(BattleResult result)
    {
        Fx.Instance.ClearUi();
        battle.Clear();
        cannon.ResetPosition();
        cannon.gameObject.SetActive(false);
        Open(resultScreen.gameObject);
        resultScreen.Show(result);
    }

    public void OpenMeta()
    {
        cannon.gameObject.SetActive(true);
        Fx.Instance.ClearUi();
        battle.Clear();
        Open(metaScreen.gameObject);
        metaScreen.Show();
    }

    void Open(GameObject screen)
    {
        startScreen.SetActive(screen == startScreen);
        hud.gameObject.SetActive(screen == hud.gameObject);
        resultScreen.gameObject.SetActive(screen == resultScreen.gameObject);
        metaScreen.gameObject.SetActive(screen == metaScreen.gameObject);
    }
}
