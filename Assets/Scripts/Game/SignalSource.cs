using UnityEngine;

public class SignalSource : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField]
    private LineRenderer signalRenderer;
    
    [Header("Setup")]
    public float maxSignalRadius;
    [SerializeField]
    private LayerMask signalSearchMask;
    
    public float signalLossPerUnitDistance = 0.1f;
    public float minDistanceToReceiveMaxSignal = 1.25f;
    
    
    Collider[] cols = new Collider[16];
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.isTransitioning)
            return;
        
        // lazy fix
        signalRenderer.SetPosition(0, transform.position);
        
         Physics.OverlapSphereNonAlloc(transform.position, maxSignalRadius, cols,signalSearchMask);
         bool foundPlayer = false;
         foreach (Collider col in cols)
         {
             if (!col)
                 continue;
             
             if (col.CompareTag("Player"))
             {
                 // TODO: Add raycasting to check if it actually has a clear path towards the player
                 
                 foundPlayer = true;
                 col.GetComponent<PlayerController>().GetWeapon().AddSignal(this);
             } else if (col.gameObject.layer == LayerMask.NameToLayer("Reflector"))
             {
                 // TODO: does it have a clear path to reflector and then reflectors bounce hits the player?
             }
         }

         if (!foundPlayer)
         {
             signalRenderer.SetPosition(1, transform.position);
         }
    }

    public void UpdateSignalVisualization(Transform signalReceiver, float dist)
    {
        float power = ((maxSignalRadius - dist) / maxSignalRadius);
        signalRenderer.SetPosition(1, signalReceiver.position);
        Color cStart = signalRenderer.startColor;
        cStart.a = 0.6f * power;
        signalRenderer.startColor = cStart;

        Color cEnd = signalRenderer.endColor;
        cEnd.a = 0.2f * power;
        signalRenderer.endColor = cEnd;
        //signalRenderer.colorGradient.alphaKeys[1].time = ((maxSignalRadius - dist) / maxSignalRadius);

    }
    
}
