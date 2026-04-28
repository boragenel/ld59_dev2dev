using System;
using DG.Tweening;
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
    private AIState currentState;

    public Vector3 targetDestination;
    public Vector3 startPos;

    public float idleTimer = 0f;

    private Rigidbody rb;

    [Header("Health")]
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
    private float stunTimer = 0f;

    private Quaternion lastZoneRot;
    // Desired world-space direction from AI state
    private Vector3 targetDir;

    // Actual steering after avoidance this physics step
    private Vector3 steeringDir;

    private float patrolGiveupTimer = 0f;
    private Vector3 lastNonZeroTargetDir = Vector3.up;

    [Header("Local Avoidance")]
    public float avoidanceCheckDistance = 0.8f;
    public LayerMask avoidanceMask;
    public Vector3 avoidanceDir = Vector3.zero;
    public Vector3 avoidanceTargetDir = Vector3.zero;
    private float avoidanceTakeOverTimer = 0f;

    public float avoidanceCooldown;

    // Zone awareness: which rotating zone we are currently in, for parenting only
    private Transform currentZone;
    public VfxInstancer deathVfx;

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

        Invoke("ResetScale", 0.5f); // temp fix

    }

    public void ResetScale()
    {
        transform.DOKill();
        transform.DOScale(1f, 0.25f);
    }

    void Update()
    {
        if (GameManager.Instance.isTransitioning) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }

        HandleCurrentZone();   // manage parenting only
        HandlePerception();
        HandleCurrentState();

    }



    void FixedUpdate()
    {
        HandleMovementPhysics();
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            SoundManager.Instance.PlayOneShot(SoundType.ENEMY_HIT, 0.5f, Random.Range(0.8f, 1.2f));
            ChangeState(AIState.CHASE_PLAYER);
        }
    }

    public void Die()
    {
        if (deathVfx != null)
        {
            deathVfx.SpawnVfx(transform.position, Quaternion.identity);
        }
        SoundManager.Instance.PlayOneShot(SoundType.ENEMY_DEATH, 0.5f, Random.Range(0.8f, 1.2f));
        Destroy(gameObject);
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
            // world-space random point around current world position
            targetDestination = transform.position + (Vector3)Random.insideUnitCircle * searchRadius;
            targetDestination.z = transform.position.z;

            if (!Physics.CheckSphere(targetDestination, 0.1f, avoidanceMask) && !Physics.Raycast(transform.position,targetDestination-transform.position,(targetDestination-transform.position).magnitude,avoidanceMask))
                break;

            searchRadius *= 1.05f;
        } while (true);
    }

    public void ChangeState(AIState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case AIState.IDLE:
                idleTimer = Random.Range(0.25f, 0.75f);
                break;

            case AIState.RANDOM_PATROL:
                PickTargetDestinationRandomly();
                patrolGiveupTimer = 0f;
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
                targetDir = Vector3.zero;

                if (idleTimer > 0f)
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
                    patrolGiveupTimer = 0f;
                }

                {
                    Vector3 diff = targetDestination - transform.position; // world space
                    diff.z = 0f;

                    if (diff.sqrMagnitude > 0.001f)
                    {
                        targetDir = diff.normalized;
                        lastNonZeroTargetDir = targetDir;
                    }
                    else
                    {
                        ChangeState(AIState.IDLE);
                    }
                }
                break;

            case AIState.CHASE_PLAYER:
                targetDestination = GameManager.Instance.player.transform.position;

                {
                    Vector3 diff2 = targetDestination - transform.position;
                    diff2.z = 0f;

                    if (diff2.sqrMagnitude > 0.001f)
                    {
                        targetDir = diff2.normalized;
                        lastNonZeroTargetDir = targetDir;
                    }
                }
                break;
        }
    }

    void HandleMovementPhysics()
    {
        if (stunTimer > 0f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Start from desired world-space direction each physics step
        steeringDir = targetDir;

        ApplyLocalAvoidance();   // modify steeringDir only

        RotateTowardsSteering();

        if (steeringDir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float moveSpeed = (chasingPlayer ? chaseSpeed : baseSpeed) +
                          (signalMeshReceiver != null ? signalMeshReceiver.SignalStrength : 0f) * extraSgnalSpeed;

        rb.linearVelocity = steeringDir.normalized * moveSpeed;
    }

    void RotateTowardsSteering()
    {
        Vector3 dirToUse = steeringDir;

        if (dirToUse.sqrMagnitude > 0.0001f)
        {
            lastNonZeroTargetDir = dirToUse.normalized;
        }
        else
        {
            dirToUse = lastNonZeroTargetDir;
        }

        if (dirToUse.sqrMagnitude < 0.0001f)
            return;

        // Rotate around world Z (forward) regardless of parent rotation
        Quaternion targetWorldRotation = Quaternion.LookRotation(Vector3.forward, dirToUse.normalized);
        Quaternion next = Quaternion.RotateTowards(rb.rotation, targetWorldRotation, rotSpeed * Time.fixedDeltaTime);

        rb.MoveRotation(next);

        Debug.DrawRay(transform.position, dirToUse.normalized, Color.purple);
        Debug.DrawRay(transform.position, targetDestination - transform.position, Color.yellow);
    }

    void ApplyLocalAvoidance()
    {
        if (avoidanceTakeOverTimer > 0f)
        {
            avoidanceTakeOverTimer -= Time.fixedDeltaTime;

            if (avoidanceDir.sqrMagnitude > 0.0001f)
            {
                steeringDir = avoidanceDir;
            }

            return;
        }

        if (steeringDir.sqrMagnitude < 0.0001f)
            return;

        Vector3 desiredDir = steeringDir.normalized;
        Vector3 origin = transform.position;
        float rayDist = avoidanceCheckDistance;

        Vector3 forwardDir = desiredDir;
        Vector3 leftDir = new Vector3(-desiredDir.y, desiredDir.x, 0f).normalized;
        Vector3 rightDir = -leftDir;

        bool hitForward = Physics.Raycast(origin, forwardDir, rayDist, avoidanceMask);
        bool hitLeft = Physics.Raycast(origin, leftDir, rayDist * 0.75f, avoidanceMask);
        bool hitRight = Physics.Raycast(origin, rightDir, rayDist * 0.75f, avoidanceMask);

        avoidanceDir = Vector3.zero;
        avoidanceTargetDir = Vector3.zero;

        if (!hitForward)
            return;

        if (hitLeft && !hitRight)
        {
            avoidanceTargetDir = rightDir;
        }
        else if (hitRight && !hitLeft)
        {
            avoidanceTargetDir = leftDir;
        }
        else
        {
            avoidanceTargetDir = leftDir;
        }

        Vector3 steered = (desiredDir * 0.4f + avoidanceTargetDir * 0.6f);
        steered.z = 0f;

        if (steered.sqrMagnitude > 0.0001f)
        {
            steered.Normalize();
            avoidanceDir = steered;
            steeringDir = steered;
            avoidanceTakeOverTimer = 0.2f;
        }

        Debug.DrawRay(origin, forwardDir * rayDist, Color.red);
        Debug.DrawRay(origin, leftDir * rayDist * 0.75f, Color.yellow);
        Debug.DrawRay(origin, rightDir * rayDist * 0.75f, Color.yellow);
        Debug.DrawRay(origin, steeringDir.normalized * rayDist, Color.cyan);
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

        bool playerInVicinity = Physics.CheckSphere(
            transform.position + transform.up * sightConeRadius * 0.5f,
            sightConeRadius,
            LayerMask.GetMask("Player")
        );

        if (playerInVicinity && needsDirectSight)
        {
            Physics.BoxCast(
                transform.position,
                new Vector3(0.1f, 0.1f, 0.1f),
                (GameManager.Instance.player.transform.position - transform.position).normalized,
                out RaycastHit hit,
                transform.rotation,
                sightConeRadius
            );

            playerInSight = hit.collider != null && hit.collider.gameObject.CompareTag("Player");
        }

        if (playerInVicinity && playerInSight)
        {
            losePlayerTimer = 0f;
            if (!chasingPlayer)
            {
                ChangeState(AIState.CHASE_PLAYER);
            }
        }
        else if (chasingPlayer)
        {
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer > 3f)
            {
                chasingPlayer = false;
                ChangeState(AIState.IDLE);
            }
        }
    }

    // ZONE AWARENESS: parenting only, no steering math
    void HandleCurrentZone()
    {
        Physics.Raycast(
            transform.position + Vector3.back * 0.5f,
            Vector3.forward,
            out RaycastHit hit,
            25f,
            LayerMask.GetMask("Zone")
        );

        Transform newZone = hit.collider != null ? hit.collider.transform.parent : null;

        if (newZone != currentZone)
        {
            currentZone = newZone;

            // Re-parent to the new zone so we ride its rotation/movement
            if (currentZone != null)
            {
                transform.SetParent(currentZone, true);
            }
            else
            {
                transform.SetParent(null, true); // no zone → parent to root
            }
        }
    }
}