using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }


    private GameSettings m_gameSettings;

    private BoardController m_boardController;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;

    public Dictionary<string, GameObject> preloadResources;

    private void Awake()
    {
        preloadResources = new Dictionary<string, GameObject>
        {
            { Constants.PREFAB_CELL_BACKGROUND, Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND) },
            { Constants.PREFAB_NORMAL_TYPE_ONE, Resources.Load<GameObject>(Constants.PREFAB_NORMAL_TYPE_ONE) },
            { Constants.PREFAB_NORMAL_TYPE_TWO, Resources.Load<GameObject>(Constants.PREFAB_NORMAL_TYPE_TWO) },
            { Constants.PREFAB_NORMAL_TYPE_THREE, Resources.Load<GameObject>(Constants.PREFAB_NORMAL_TYPE_THREE) },
            { Constants.PREFAB_NORMAL_TYPE_FOUR, Resources.Load<GameObject>(Constants.PREFAB_NORMAL_TYPE_FOUR) },
            { Constants.PREFAB_NORMAL_TYPE_FIVE, Resources.Load<GameObject>(Constants.PREFAB_NORMAL_TYPE_FIVE) },
            { Constants.PREFAB_NORMAL_TYPE_SIX, Resources.Load<GameObject>(Constants.PREFAB_NORMAL_TYPE_SIX) },
            { Constants.PREFAB_NORMAL_TYPE_SEVEN, Resources.Load<GameObject>(Constants.PREFAB_NORMAL_TYPE_SEVEN) },
            { Constants.PREFAB_BONUS_HORIZONTAL, Resources.Load<GameObject>(Constants.PREFAB_BONUS_HORIZONTAL) },
            { Constants.PREFAB_BONUS_VERTICAL, Resources.Load<GameObject>(Constants.PREFAB_BONUS_VERTICAL) },
            { Constants.PREFAB_BONUS_BOMB, Resources.Load<GameObject>(Constants.PREFAB_BONUS_BOMB) },
        };
        
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);

        if (mode == eLevelMode.MOVES)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), m_boardController);
        }
        else if (mode == eLevelMode.TIMER)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), this);
        }

        m_levelCondition.ConditionCompleteEvent += GameOver;

        State = eStateGame.GAME_STARTED;
    }

    public void GameOver()
    {
        StartCoroutine(WaitBoardController());
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }
}
