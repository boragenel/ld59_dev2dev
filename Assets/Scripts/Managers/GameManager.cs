using DG.Tweening;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public enum GamePhase
{
    NONE,
    CUTSCENE,
    LOADOUT,
    SHOP,
    GAMEPLAY
}

public class GameManager : MonoBehaviour
{
    public bool TEST_START;

    [Header("Managers")]
    public SoundManager soundManager;
    public UIManager uiManager;

    public static GameManager Instance;
    [Header("Game Phase")]
    public GamePhase startingGamePhase = GamePhase.NONE;
    [SerializeField]
    [ReadOnlyAttribute]
    private GamePhase currentGamePhase = GamePhase.NONE;

    [Header("Game Dynamics")]
    public bool isTransitioning = false;

    [Header("Key Objects")]
    public PlayerController player;

    ///
    /// EVENTS
    public static UnityAction OnLevelFailed;
    public static UnityAction OnLevelCompleted;
    public static UnityAction OnLevelClear;

    [Header("Levels")]
    public List<LevelBase> LevelOrderPrefabs;
    public int currentLevelIndex = 0; //if it ends up being endless need to handle overflow and picking a random one with some difficulty modifiers, maybe enemy speed is extra for every value above the list count?

    [ReadOnly] public LevelBase currentLevel;

    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
        }
        Instance = this;

        ChangeGameState(startingGamePhase);
        //SoundManager.Instance.PlayOneShot(SoundType.KISS,1f,Random.Range(0.8f,1.2f));
    }
    private void Start()
    {
        if (TEST_START)
        {
            StartNewGame();
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public GamePhase GetCurrentGamePhase()
    {
        return currentGamePhase;
    }

    public void ChangeGameState(GamePhase newGamePhase)
    {
        currentGamePhase = newGamePhase;

        switch (currentGamePhase)
        {
            case GamePhase.GAMEPLAY:
                break;
        }
    }

    #region Level Handling
    public void StartNewGame()
    {
        if (currentLevel != null)
        {
            Destroy(currentLevel.gameObject);
        }

        currentLevelIndex = 0;
        currentLevel = LevelOrderPrefabs[0];
        currentLevel = Instantiate(currentLevel.gameObject).GetComponent<LevelBase>();

        if (!currentLevel.DontRotateOnInit)
            currentLevel.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        PlayerController.Instance.gameObject.SetActive(true);
        SetPlayerToStartPos();
        GameManager.Instance.isTransitioning = false;
    }
    public GameObject GetNextLevelPrefab()
    {
        currentLevelIndex++;
        if (currentLevelIndex >= LevelOrderPrefabs.Count) //if not endless gotta fix this to spawn a final level that triggers victory screen?
        {
            currentLevelIndex = 0;
        }
        return LevelOrderPrefabs[currentLevelIndex].gameObject;
    }
    public void SetPlayerOnEntranceGate()
    {

    }
    public void TriggerGameOverSequence()
    {
        Debug.Log("GAME OVER");
        StartCoroutine(GameOverSequence());
        IEnumerator GameOverSequence()
        {
            PlayerController.Instance.gameObject.SetActive(false);
            PlayerController.Instance.transform.SetParent(null, true);
            GameManager.Instance.isTransitioning = true;
            GameManager.OnLevelClear?.Invoke();
            //put destruction effects here, time delay, loading screen, idfks
            yield return new WaitForSecondsRealtime(0.5f);
            StartNewGame();
        }
    }

    public void SetPlayerToStartPos()
    {
        Transform entranceGate = GameManager.Instance.currentLevel.levelEntrance.transform;
        PlayerController pc = PlayerController.Instance;
        if (entranceGate)
        {
            Vector3 pos = entranceGate.position;
            pos.z = 0;
            pc.transform.position = pos;
            pc.transform.DOScale(1f, 0.25f).OnComplete(() =>
            {
                GameManager.Instance.isTransitioning = false;
                pc.controlsEnabled = true;
                pc.collision.SetActive(true);
                pos = pc.transform.localPosition;
                pos.z = 0;
                pc.transform.localPosition = pos;
                entranceGate.DOScale(0, 0.5f).SetDelay(0.4f);
            });
        }
        else
        {
            Debug.LogError("Entrance gate not found");
        }
    }
    #endregion
}
