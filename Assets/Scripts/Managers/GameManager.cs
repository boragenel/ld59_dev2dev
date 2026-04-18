using NUnit.Framework;
using System;
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
    public static UnityAction OnLevelComplete;

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
        currentLevelIndex = 0;
        currentLevel = LevelOrderPrefabs[0];
        currentLevel = Instantiate(currentLevel.gameObject).GetComponent<LevelBase>();
        // StartLevel();
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
    #endregion
}
