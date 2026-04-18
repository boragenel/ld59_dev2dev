using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum AIState
{
    NONE,
    IDLE,
    RANDOM_PATROL,
    CHASE_PLAYER
}

public enum EnemyType
{
    NONE,
    BASIC_ENEMY
}


public class Enemy : MonoBehaviour
{

    public float moveSpeed = 4;
    public EnemyType enemyType;

    [SerializeField]
    [ReadOnlyAttribute]
    private AIState currentState;

    public Vector3 targetDestination;
    public Vector3 startPos;

    public float idleTimer = 0f;

    private Rigidbody rbody;

    [Header("Setup")] 
    public bool needsDirectSight = true;
    public float sightConeRadius = 3f;
    public float chaseSpeedMult = 1.5f;
    
    [Header("Dynamics")]
    private bool chasingPlayer = false;
    private float losePlayerTimer = 0f;
    private float stunTimer = 0;
    private Vector3 targetDir;
    private float patrolGiveupTimer = 0f;


    [Header("Local Avoidance")] 
    public float avoidanceCheckDistance = 0.8f;
    public LayerMask avoidanceMask;
    public Vector3 avoidanceDir = Vector3.zero;
    public Vector3 avoidanceTargetDir = Vector3.zero;

    public float avoidanceCooldown;
    //private float avoidance
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        switch (enemyType)
        {
            case EnemyType.BASIC_ENEMY:
                ChangeState(AIState.IDLE);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
        }
        //HandleCurrentZone();
        HandlePerception();
        HandleCurrentState();
        

    }

    public AIState GetCurrentState()
    {
        return currentState;
    }

    public void PickTargetDestinationRandomly()
    {
        targetDestination = transform.localPosition + (Vector3)Random.insideUnitCircle * 3f;
    }

    public void ChangeState(AIState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case AIState.IDLE:
                idleTimer = Random.Range(0.5f, 2f);
                break;
            case AIState.RANDOM_PATROL:
                PickTargetDestinationRandomly();
                break;
            case AIState.CHASE_PLAYER:
                chasingPlayer = true;
                break;
        }
    }

    void HandleCurrentState()
    {
        switch (currentState)
        {
            case AIState.IDLE:
                if (idleTimer > 0)
                {
                    idleTimer -= Time.deltaTime;
                    if (idleTimer <= 0f)
                    {
                        DecideIdleExitAction();
                    }
                }
                break;
            case AIState.RANDOM_PATROL:
                patrolGiveupTimer += Time.deltaTime;
                if (patrolGiveupTimer > 2.5f)
                {
                    PickTargetDestinationRandomly();
                    patrolGiveupTimer = 0;
                }
                
                Vector3 diff = targetDestination - transform.localPosition;
                Vector3 worldDir = transform.parent.TransformDirection(diff);
                
                if (worldDir.sqrMagnitude > 1f)
                {
                    worldDir.z = 0;
                    targetDir = worldDir;
                    MoveTowardsDirection();
                }
                else
                {
                    ChangeState(AIState.IDLE);
                }
                break;
            case AIState.CHASE_PLAYER:
                targetDestination = transform.parent.InverseTransformPoint(GameManager.Instance.player.transform.position);
                Vector3 diff2 = (targetDestination - transform.localPosition);
                Vector3 worldDirection2 = transform.parent.TransformDirection(diff2);
                targetDir = worldDirection2;
                MoveTowardsDirection();
                break;
        }
    }

    void MoveTowardsDirection()
    {
        //Debug.Log("MoveTowardsDirection");
        if (stunTimer > 0)
            return;

        RotateTowardsTarget();
        
        float mult = 1f;
        if (chasingPlayer)
        {
            mult = chaseSpeedMult;
        }
        
        transform.position += targetDir.normalized * moveSpeed * mult * Time.deltaTime;
        rbody.position = transform.position;
    }

    void RotateTowardsTarget()
    {
        //Debug.Log("RotateTowardsTarget");
        HandleLocalAvoidance();
        
        Quaternion rot = transform.rotation;
        transform.up = targetDir;
        transform.rotation = Quaternion.Slerp(rot,transform.rotation, Time.deltaTime * 5f);
    }
    

    void HandleLocalAvoidance()
    {
        //Debug.Log("HandleLocalAvoidance");
        avoidanceCooldown -= Time.deltaTime;
        
        RaycastHit[] collisions = new RaycastHit[10];
        Physics.SphereCastNonAlloc(transform.position,0.25f,transform.up, collisions, avoidanceCheckDistance, avoidanceMask);
        foreach(RaycastHit hit in collisions)
        {
            if (hit.collider == null)
                continue;
            
            if (hit.collider.gameObject == gameObject)
            {
                continue;
            }

            if (avoidanceCooldown <= 0)
            {
                avoidanceTargetDir = hit.normal;
                avoidanceCooldown = 0.5f;
            }
            
            
            float rotSpeed = 600f;
            Quaternion targetRot = Quaternion.LookRotation(transform.forward,avoidanceTargetDir);
            Quaternion rot = Quaternion.RotateTowards(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
            targetDir = rot * Vector3.up;
            break;

        }
    }
    
    void DecideIdleExitAction()
    {
        switch (enemyType)
        {
            case EnemyType.BASIC_ENEMY:
                ChangeState(AIState.RANDOM_PATROL);
                break;
        }
    }

    void HandlePerception()
    {
        bool playerInSight = !needsDirectSight;
        
        bool playerInVicinity = Physics.CheckSphere(transform.position + transform.up * sightConeRadius* 0.5f , sightConeRadius, LayerMask.GetMask("Player"));

        if (playerInVicinity && needsDirectSight)
        {
            Physics.BoxCast(transform.position, new Vector3(0.1f, 0.1f, 0.1f),
                (GameManager.Instance.player.transform.position - transform.position).normalized, out RaycastHit hit,
                transform.rotation, sightConeRadius);
            playerInSight = hit.collider != null && hit.collider.gameObject.CompareTag("Player");
        }

        if (playerInVicinity && playerInSight)
        {
            losePlayerTimer = 0;
            if (!chasingPlayer)
            {
                ChangeState(AIState.CHASE_PLAYER);
            }
        }
        else if (chasingPlayer)
        {
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer > 1f)
            {
                chasingPlayer = false;
                ChangeState(AIState.IDLE);
            }
        }
        
    }
    
    void HandleCurrentZone()
    {
        Physics.Raycast(transform.position+Vector3.back*0.5f, Vector3.forward, out RaycastHit hit, 25, LayerMask.GetMask("Zone"));
        if (hit.collider != null)
        {
            if (transform.parent != hit.transform.parent)
            {
                transform.SetParent(hit.transform.parent,true);
            }
        }
    }

    
}
