using System;
using Unity.VisualScripting;
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
    public EnemyType enemyType;
    private SignalMeshPointReceiver signalMeshReceiver;

    [SerializeField]
    [ReadOnlyAttribute]
    private AIState currentState;

    public Vector3 targetDestination;
    public Vector3 startPos;

    public float idleTimer = 0f;

    private Rigidbody rb; //hehe

    [Header("Speed")]
    public float maxHp;
    public float currentHP;

    [Header("Speed")]
    public float baseSpeed;
    public float chaseSpeed;
    public float extraSgnalSpeed;
    public float rotSpeed;

    [Header("Setup")]
    public bool needsDirectSight = true;
    public float sightConeRadius = 3f;

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
    private float avoidanceTakeOverTimer = 0f;

    public float avoidanceCooldown;
    //private float avoidance

    //Pomo notes
    //I tried cleaning this up a bit to work with physics, not gonna have time to fully polish it unfortunetly, might be good enough as is but well see

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHP = maxHp;
        rb = GetComponent<Rigidbody>();
        signalMeshReceiver = GetComponentInChildren<SignalMeshPointReceiver>();

        switch (enemyType)
        {
            case EnemyType.BASIC_ENEMY:
                ChangeState(AIState.IDLE);
                break;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
        }
        //dsnt the enemy need this back on?
        HandleCurrentZone(); 
        HandlePerception();
        HandleCurrentState();
    }

    public AIState GetCurrentState()
    {
        return currentState;
    }

    public void PickTargetDestinationRandomly()
    {

        float searchRadius = 2f;
        do
        {
            targetDestination = transform.localPosition + (Vector3)Random.insideUnitCircle * searchRadius;
            if (!Physics.CheckSphere(targetDestination, 0.1f, avoidanceMask))
                break;
            searchRadius *= 1.05f;
        } while (true);
        
        targetDestination.z = transform.position.z;
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

                Vector3 diff = targetDestination - transform.position;
                //Vector3 worldDir = transform.parent.TransformDirection(diff);
                Vector3 worldDir = diff;

                if (worldDir.sqrMagnitude > 1f)
                {
                    worldDir.z = 0;
                    if(avoidanceTakeOverTimer <= 0)
                        targetDir = worldDir;
                    
                    MoveTowardsDirection();
                }
                else
                {
                    ChangeState(AIState.IDLE);
                }
                break;
            case AIState.CHASE_PLAYER:
                //targetDestination = transform.parent.InverseTransformPoint(GameManager.Instance.player.transform.position);
                targetDestination = GameManager.Instance.player.transform.position;
                Vector3 diff2 = (targetDestination - transform.position);
                //Vector3 worldDirection2 = transform.parent.TransformDirection(diff2);
                Vector3 worldDirection2 = diff2.normalized;
                worldDirection2.z = 0;
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

        //float mult = 1f;
        //if (chasingPlayer)
        //{
        //    mult = chaseSpeedMult;
        //}

        //transform.position += targetDir.normalized * moveSpeed * mult * Time.deltaTime;
        //rb.position = transform.position;

        if (targetDir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        //i know this is on UPDATE and not on FIXED update but is just setting the velocity of the RB so it should be fiiiiiiiiiinne?
        //Also it NEEDS fixed delta time so we can maybe speed up the game later as a endless thing and or switch the frequency of fixed delta time
        rb.linearVelocity = targetDir.normalized * ((chasingPlayer ? chaseSpeed : baseSpeed) + (signalMeshReceiver != null ? signalMeshReceiver.SignalStrength : 0) * extraSgnalSpeed);
    }

    void RotateTowardsTarget()
    {
        //Debug.Log("RotateTowardsTarget");

        //This was causing some weird issues so i turned it off for now until we get the game feeling better
        HandleLocalAvoidance();

        Quaternion targetRotation = Quaternion.LookRotation(transform.forward, targetDir);
        Quaternion rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotSpeed);
        rb.rotation = rotation;
        
        Debug.DrawRay(transform.position, targetDir, Color.purple);
        Debug.DrawRay(transform.position, targetDestination-transform.position, Color.yellow);
    }

    //This was causing some weird issues so i turned it off for now until we get the game feeling better
    void HandleLocalAvoidance()
    {
        //Debug.Log("HandleLocalAvoidance");
        avoidanceCooldown -= Time.deltaTime;
        avoidanceTakeOverTimer -= Time.deltaTime;

        RaycastHit[] collisions = new RaycastHit[10];
        Physics.SphereCastNonAlloc(transform.position+transform.up*0.1f, 0.1f, transform.up, collisions, avoidanceCheckDistance, avoidanceMask);
        foreach (RaycastHit hit in collisions)
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
                avoidanceTakeOverTimer = 0.5f;
            }
            
            Quaternion targetRot = Quaternion.LookRotation(transform.forward, avoidanceTargetDir);
            Quaternion rot = Quaternion.RotateTowards(transform.rotation, targetRot, 90);
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

        bool playerInVicinity = Physics.CheckSphere(transform.position + transform.up * sightConeRadius * 0.5f, sightConeRadius, LayerMask.GetMask("Player"));

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
        Physics.Raycast(transform.position + Vector3.back * 0.5f, Vector3.forward, out RaycastHit hit, 25, LayerMask.GetMask("Zone"));
        if (hit.collider != null)
        {
            if (transform.parent != hit.transform.parent)
            {
                transform.SetParent(hit.transform.parent, true);
            }
        }
    }


}
