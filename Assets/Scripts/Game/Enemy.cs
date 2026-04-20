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
    private Vector3 targetDir;
    private float patrolGiveupTimer = 0f;

    private Transform currentZone;
    private Quaternion lastZoneRotation;

    private Vector3 lastNonZeroTargetDir = Vector3.up;

    [Header("Local Avoidance")]
    public float avoidanceCheckDistance = 0.8f;
    public LayerMask avoidanceMask;
    public Vector3 avoidanceDir = Vector3.zero;
    public Vector3 avoidanceTargetDir = Vector3.zero;
    private float avoidanceTakeOverTimer = 0f;

    public float avoidanceCooldown;

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

    void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }

        HandleCurrentZone();
        HandlePerception();
        HandleCurrentState();
    }

    void FixedUpdate()
    {
        //HandleZoneRotation();
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
            ChangeState(AIState.CHASE_PLAYER);
        }
    }

    public void Die()
    {
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
            targetDestination = transform.position + (Vector3)Random.insideUnitCircle * searchRadius;
            targetDestination.z = transform.position.z;

            if (!Physics.CheckSphere(targetDestination, 0.1f, avoidanceMask))
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
                idleTimer = Random.Range(0.5f, 2f);
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

                Vector3 diff = targetDestination - transform.position;
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
                break;

            case AIState.CHASE_PLAYER:
                targetDestination = GameManager.Instance.player.transform.position;

                Vector3 diff2 = targetDestination - transform.position;
                diff2.z = 0f;

                if (diff2.sqrMagnitude > 0.001f)
                {
                    targetDir = diff2.normalized;
                    lastNonZeroTargetDir = targetDir;
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

        RotateTowardsTarget();

        Vector3 moveDir = targetDir;

        if (moveDir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float moveSpeed = (chasingPlayer ? chaseSpeed : baseSpeed) +
                          (signalMeshReceiver != null ? signalMeshReceiver.SignalStrength : 0f) * extraSgnalSpeed;

        rb.linearVelocity = moveDir.normalized * moveSpeed;
    }

    void RotateTowardsTarget()
    {
        HandleLocalAvoidance();

        Vector3 dirToUse = targetDir;

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

        Quaternion targetWorldRotation = Quaternion.LookRotation(Vector3.forward, dirToUse.normalized);
        Quaternion next = Quaternion.RotateTowards(rb.rotation, targetWorldRotation, rotSpeed * Time.fixedDeltaTime);

        rb.MoveRotation(next);

        Debug.DrawRay(transform.position, dirToUse.normalized, Color.purple);
        Debug.DrawRay(transform.position, targetDestination - transform.position, Color.yellow);
    }

    void HandleLocalAvoidance()
    {
        if (avoidanceTakeOverTimer > 0f)
        {
            avoidanceTakeOverTimer -= Time.fixedDeltaTime;

            if (avoidanceDir.sqrMagnitude > 0.0001f)
            {
                targetDir = avoidanceDir;
            }

            return;
        }

        if (targetDir.sqrMagnitude < 0.0001f)
            return;

        Vector3 desiredDir = targetDir.normalized;
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
            targetDir = steered;
            avoidanceTakeOverTimer = 0.2f;
        }

        Debug.DrawRay(origin, forwardDir * rayDist, Color.red);
        Debug.DrawRay(origin, leftDir * rayDist * 0.75f, Color.yellow);
        Debug.DrawRay(origin, rightDir * rayDist * 0.75f, Color.yellow);
        Debug.DrawRay(origin, targetDir.normalized * rayDist, Color.cyan);
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
            if (losePlayerTimer > 2f)
            {
                chasingPlayer = false;
                ChangeState(AIState.IDLE);
            }
        }
    }

    void HandleCurrentZone()
    {
        Physics.Raycast(
            transform.position + Vector3.back * 0.5f,
            Vector3.forward,
            out RaycastHit hit,
            25f,
            LayerMask.GetMask("Zone")
        );

        if (hit.collider != null)
        {
            Transform newZone = hit.transform.parent;

            if (currentZone != newZone)
            {
                currentZone = newZone;
                if (currentZone != null)
                    lastZoneRotation = currentZone.rotation;
            }
        }
        else
        {
            currentZone = null;
        }
    }

    void HandleZoneRotation()
    {
        if (currentZone == null)
            return;

        Quaternion deltaRotation = currentZone.rotation * Quaternion.Inverse(lastZoneRotation);

        Vector3 offset = rb.position - currentZone.position;
        offset = deltaRotation * offset;

        rb.MovePosition(currentZone.position + offset);

        lastZoneRotation = currentZone.rotation;
    }
}