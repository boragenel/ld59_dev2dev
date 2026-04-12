using System;
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
    [Header("Managers")]
    public SoundManager soundManager;
    public UIManager uiManager;

    public static GameManager Instance;
    [Header("Game Phase")]
    public GamePhase startingGamePhase = GamePhase.NONE;
    [SerializeField]
    [ReadOnlyAttribute]
    private GamePhase currentGamePhase = GamePhase.NONE;
    //[Header("Game Dynamics")]
    
    
    ///
    /// EVENTS
    public static UnityAction OnLevelFailed;
    public static UnityAction OnLevelComplete;
    
    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
        }
        Instance = this;
        
        ChangeGameState(startingGamePhase);
        //SoundManager.Instance.PlayOneShot(SoundType.KISS,1f,Random.Range(0.8f,1.2f));

        SpriteRenderer spriteFromPool = PoolManager.DequeueObejct<SpriteRenderer>(PoolerType.GENERIC_SPRITE);
        spriteFromPool.transform.position = Random.insideUnitCircle;

        
        
        
        
        PoolManager.EnqueueObject(spriteFromPool,PoolerType.GENERIC_SPRITE);
        
        
        
        
        
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
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
