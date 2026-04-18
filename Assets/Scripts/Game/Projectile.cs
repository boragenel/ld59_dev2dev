using UnityEngine;


public enum ProjectileBehaviourType
{
    NONE,
    REGULAR,
    HOMING,
    BOUNCE
}

public class Projectile : MonoBehaviour
{
    public ProjectileBehaviourType behaviourType;
    
    public float speed = 4f;
    public float damage= 1f;

    public float enemySearchRadius = 0.1f;
    public LayerMask enemySearchMask;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandleProjectileBehaviour();
        HandleCollisions();
    }

    void HandleProjectileBehaviour()
    {
        switch (behaviourType)
        {
            case ProjectileBehaviourType.REGULAR:
                transform.position += transform.up * speed * Time.deltaTime;
                break;
        }
    }

    void HandleCollisions()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, enemySearchRadius, enemySearchMask);
        if (cols.Length > 0)
        {
            Debug.Log("Projectile death");
            PoolManager.EnqueueObject(this,PoolerType.PLAYER_BULLET);
        }
    }
    
}
