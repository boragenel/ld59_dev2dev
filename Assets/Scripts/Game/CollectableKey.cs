using UnityEngine;

public class CollectableKey : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.currentLevel.OnCollectKey();
            Destroy(gameObject);
        }
    }
}
