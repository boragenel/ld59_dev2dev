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
    public VfxInstancer hitVfx;

    public float speed = 4f;
    public float damage = 1f;

    public float enemySearchRadius = 0.1f;
    public LayerMask enemySearchMask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        HandleProjectileBehaviour();
        HandleCollisions();
    }

    void HandleProjectileBehaviour()
    {
        switch (behaviourType)
        {
            case ProjectileBehaviourType.REGULAR:
                transform.position += transform.up * speed * Time.fixedDeltaTime;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {

        PoolManager.EnqueueObject(this, PoolerType.PLAYER_BULLET);
    }
    void HandleCollisions()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, enemySearchRadius, enemySearchMask);
        if (cols.Length > 0)
        {
            foreach (Collider col in cols)
            {
                if (col.CompareTag("Enemy"))
                {
                   // Debug.Log("Hit enemy");
                    Enemy e = col.GetComponentInParent<Enemy>();
                    e.TakeDamage(damage);
                }
                else if (col.gameObject.layer == LayerMask.NameToLayer("Glass"))
                {
                    SoundManager.Instance.PlayOneShot(SoundType.GLASS_HIT,0.25f,Random.Range(0.8f,1.2f));
                }
            }
            //Debug.Log("Projectile death");
            if (hitVfx != null)
            {
                hitVfx.SpawnVfx(transform.position, Quaternion.identity);
            }
            PoolManager.EnqueueObject(this, PoolerType.PLAYER_BULLET);
            SoundManager.Instance.PlayOneShot(SoundType.BULLET_BOUNCE,0.1f,Random.Range(0.7f,1.3f));
        }
    }

}
