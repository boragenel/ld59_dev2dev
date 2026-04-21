using DG.Tweening;
using UnityEngine;

public class CollectableKey : MonoBehaviour
{
    public VfxInstancer collectVfx;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponent<Collider>().enabled = false;
            SoundManager.Instance.PlayOneShot(SoundType.GATE_KEY_ACQUIRED, 0.5f, Random.Range(0.9f, 1.1f));
            transform.DOBlendableLocalRotateBy(Vector3.one * 720, 1f);
            transform.DOMove(GameManager.Instance.currentLevel.levelExit.transform.position + Vector3.back * 3f, 0.7f).OnComplete(() =>
            {
                transform.DOShakePosition(0.3f, Vector3.one * 0.25f, 30).OnComplete(() =>
                {
                    Exit();
                });
            });

        }
    }

    public void Exit()
    {
        GameManager.Instance.currentLevel.OnCollectKey();
        if (collectVfx != null)
        {
            collectVfx.SpawnVfx(transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

}
