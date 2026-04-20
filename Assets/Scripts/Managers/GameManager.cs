using DG.Tweening;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

public enum GamePhase {
    NONE,
    CUTSCENE,
    LOADOUT,
    SHOP,
    GAMEPLAY,
    BUILDING,
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
    public bool LosEntrancePresentationComplete { get; private set; } = true;
    [SerializeField]
    private float signalLosFadeOutDuration = 0.2f;
    [SerializeField]
    private float signalLosFadeInDuration = 0.25f;
    [SerializeField]
    private float signalLosFadeInDelay = 1f;

    public float gameTimer = 0f;

    [Header("Key Objects")]
    public PlayerController player;
    public SpriteRenderer transitionDepths;

    ///
    /// EVENTS
    public static UnityAction OnLevelFailed;
    public static UnityAction OnLevelCompleted;
    public static UnityAction OnLevelClear;

    [Header("Levels")]
    public List<LevelBase> LevelOrderPrefabs;
    public int currentLevelIndex = 0; //if it ends up being endless need to handle overflow and picking a random one with some difficulty modifiers, maybe enemy speed is extra for every value above the list count?

    [ReadOnly] public LevelBase currentLevel;

    private float signalLosFadeValue = 1f;
    private Tween signalLosFadeTween;
    private Tween signalLosFadeInDelayTween;

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
        KillSignalLosFadeTween();
        Instance = null;
    }

    private void KillSignalLosFadeTween() {
        if (signalLosFadeTween != null && signalLosFadeTween.IsActive()) {
            signalLosFadeTween.Kill();
        }
        signalLosFadeTween = null;
        if (signalLosFadeInDelayTween != null && signalLosFadeInDelayTween.IsActive()) {
            signalLosFadeInDelayTween.Kill();
        }
        signalLosFadeInDelayTween = null;
    }

    private void ScheduleSignalLosFadeInAfterDelay(System.Action onComplete) {
        if (signalLosFadeInDelayTween != null && signalLosFadeInDelayTween.IsActive()) {
            signalLosFadeInDelayTween.Kill();
        }
        signalLosFadeInDelayTween = DOVirtual.DelayedCall(signalLosFadeInDelay, () => {
            signalLosFadeInDelayTween = null;
            PlaySignalLosFadeIn(onComplete);
        }).SetTarget(this);
    }

    public void PlaySignalLosFadeOut() {
        LosEntrancePresentationComplete = false;
        KillSignalLosFadeTween();
        signalLosFadeTween = DOTween.To(() => signalLosFadeValue, v => {
            signalLosFadeValue = v;
            SignalMeshFieldManager.Instance?.ApplyLosFadeToAllSources(v);
        }, 0f, signalLosFadeOutDuration).SetTarget(this);
    }

    public void PlaySignalLosFadeIn(System.Action onComplete) {
        KillSignalLosFadeTween();
        signalLosFadeTween = DOTween.To(() => signalLosFadeValue, v => {
            signalLosFadeValue = v;
            SignalMeshFieldManager.Instance?.ApplyLosFadeToAllSources(v);
        }, 1f, signalLosFadeInDuration).SetTarget(this).OnComplete(() => {
            signalLosFadeTween = null;
            LosEntrancePresentationComplete = true;
            onComplete?.Invoke();
        });
    }

    public GamePhase GetCurrentGamePhase()
    {
        return currentGamePhase;
    }

    public void ChangeGameState(GamePhase newGamePhase) {
        currentGamePhase = newGamePhase;
        switch (currentGamePhase) {
            case GamePhase.BUILDING:
                BuildManager.Instance?.SetBuildModeEnabled(true);
                //SetPlayerBuildLocked(true);
                break;
            case GamePhase.GAMEPLAY:
                BuildManager.Instance?.SetBuildModeEnabled(true);
                SetPlayerBuildLocked(true);
                break;
            default:
                BuildManager.Instance?.SetBuildModeEnabled(true);
                break;
        }
    }

    public void BeginGameplayFromBuild() {
        LosEntrancePresentationComplete = true;
        ChangeGameState(GamePhase.GAMEPLAY);
    }

    void Update()
    {
        gameTimer += Time.deltaTime;
        uiManager.UpdateSpeedRunTimer(gameTimer);
    }
    
    private void SetPlayerBuildLocked(bool lockedForBuild) {
        if (player == null) {
            player = PlayerController.Instance;
        }
        if (player == null) {
            return;
        }
        player.controlsEnabled = !lockedForBuild;
        if (lockedForBuild) {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.linearVelocity = Vector3.zero;
            }
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

        PlayerController.Instance.gameObject.SetActive(true);
        //ChangeGameState(GamePhase.BUILDING);
        SetPlayerToStartPos();
    }

    public void RestartLevel()
    {
        if (currentLevel != null)
        {
            Destroy(currentLevel.gameObject);
        }
        
        currentLevel = LevelOrderPrefabs[currentLevelIndex];
        currentLevel = Instantiate(currentLevel.gameObject).GetComponent<LevelBase>();

        PlayerController.Instance.gameObject.SetActive(true);
        //ChangeGameState(GamePhase.BUILDING);
        SetPlayerToStartPos();
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
            PlayerController.Instance.controlsEnabled = false;
            PlayerController.Instance.gameObject.SetActive(false);
            PlayerController.Instance.transform.SetParent(null, true);
            SetZoneRotationsEnabled(false);
            GameManager.Instance.isTransitioning = true;
            GameManager.Instance.PlaySignalLosFadeOut();
            GameManager.OnLevelClear?.Invoke();
            //put destruction effects here, time delay, loading screen, idfks
            yield return new WaitForSecondsRealtime(0.5f);
            SetZoneRotationsEnabled(true);
            RestartLevel();
        }
    }

    public void SetZoneRotationsEnabled(bool value)
    {
        foreach (Transform t in currentLevel.transform)
        {
            AutoMoveAndRotate amr = t.GetComponent<AutoMoveAndRotate>();
            if (amr)
            {
                amr.enabled = value;    
            }
            
        }
    }
    

    public void SetPlayerToStartPos(bool instant=false)
    {
        if (!instant)
        {
            LosEntrancePresentationComplete = false;
            SetZoneRotationsEnabled(false);    
        }
        
        player.ResetRigidbody();
        Transform entranceGate = GameManager.Instance.currentLevel.levelEntrance.transform;
        PlayerController pc = PlayerController.Instance;
        if (entranceGate)
        {
            Vector3 pos = entranceGate.position;
            pos.z = 0;
            pc.transform.position = pos;
            pc.transform.DOScale(1f, 0.25f).OnComplete(() => {
                GameManager.Instance.isTransitioning = false;
                pc.controlsEnabled = GameManager.Instance != null && GameManager.Instance.GetCurrentGamePhase() == GamePhase.GAMEPLAY;
                pc.collision.SetActive(true);
                pc.ResetRigidbody();
                pos = pc.transform.localPosition;
                pos.z = 0;
                pc.transform.localPosition = pos;
                GameManager.Instance.ScheduleSignalLosFadeInAfterDelay(() => {
                    SetZoneRotationsEnabled(true);
                    entranceGate.DOScale(0, 0.5f).SetDelay(0.1f);
                });
            });
        }
        else
        {
            Debug.LogError("Entrance gate not found");
            isTransitioning = false;
            ScheduleSignalLosFadeInAfterDelay(null);
        }
    }
    #endregion
}
