using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class SignalSource : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform signalRendererHolder;

    //[SerializeField]
    //private LineRenderer signalRenderer;

    [Header("Setup")]
    public float maxSignalRadius;
    //TODO Add visual indicator for signal radius, semi transparent circle?

    [SerializeField]
    private LayerMask signalSearchMask;

    public float signalLossPerUnitDistance = 0.1f;
    public float minDistanceToReceiveMaxSignal = 1.25f;

    //increase this when it inevitably is not enough
    Collider[] cols = new Collider[16];

    // Update is called once per frame
    //Why is this not on fixed update? im changing it
    //Allowing multiple signal receivers so we can power up enemies and obstacles too etc
    private HashSet<SignalReceptor> receivers = new HashSet<SignalReceptor>();
    private Dictionary<SignalReceptor, SignalRenderer> signalRenderers = new Dictionary<SignalReceptor, SignalRenderer>();

    //private void Update()
    //{
    //    //add updating signal renderers
    //    //lazy fix
    //    //signalRenderer.SetPosition(0, transform.position);

    //    //foreach (KeyValuePair<SignalReceptor, SignalRenderer> signalRender in signalRenderers)
    //    //{
    //    //    UpdateSignalVisualization(signalRender.Value, signalRender.Key.transform, Vector3.Distance(transform.position, signalRender.Key.transform.position));
    //    //}
    //}
    void FixedUpdate()
    {
        if (GameManager.Instance.isTransitioning)
            return;

        receivers.Clear();

        Physics.OverlapSphereNonAlloc(transform.position, maxSignalRadius, cols, signalSearchMask);
        bool foundPlayer = false;
        foreach (Collider col in cols)
        {
            if (!col)
                continue;

            //only supporting one signal receiver per collider, not sure if need more?
            if (col.TryGetComponent<SignalReceptor>(out SignalReceptor signalReceiver))
            {
                // TODO: Add raycasting to check if it actually has a clear path towards the receiver
                foundPlayer = true;
                receivers.Add(signalReceiver);
            }

            //else if (col.gameObject.layer == LayerMask.NameToLayer("Reflector"))
            //{
            //    // TODO: does it have a clear path to reflector and then reflectors bounce hits the player?
            //}
        }
        foreach (var signalReceiver in receivers)
        {
            signalReceiver.AddSignal(this);
            if (!signalRenderers.ContainsKey(signalReceiver))
            {
                SignalRenderer newRenderer = PoolManager.DequeueObject<SignalRenderer>(PoolerType.SIGNAL_RENDERER);
                newRenderer.transform.SetParent(signalRendererHolder.transform, false);
                newRenderer.transform.localPosition = Vector3.zero;
                newRenderer.gameObject.SetActive(true);
                signalRenderers.Add(signalReceiver, newRenderer);
            }
        }
        foreach (KeyValuePair<SignalReceptor, SignalRenderer> signalRender in signalRenderers)
        {
            if (!receivers.Contains(signalRender.Key))
            {
                signalRender.Value.gameObject.SetActive(true);
                PoolManager.EnqueueObject(signalRender.Value, PoolerType.SIGNAL_RENDERER);
                signalRenderers.Remove(signalRender.Key);
            }
        }
    }

    //Todo, change color based on signal receiver type?
    public void UpdateSignalVisualization(SignalReceptor receiver, Transform signalReceiver, float dist)
    {
        var renderer = signalRenderers[receiver];

        renderer.lineRenderer.SetPosition(0, transform.position);

        float power = ((maxSignalRadius - dist) / maxSignalRadius);
        renderer.lineRenderer.SetPosition(1, signalReceiver.position);
        Color cStart = renderer.lineRenderer.startColor;
        cStart.a = 0.6f * power;
        renderer.lineRenderer.startColor = cStart;

        Color cEnd = renderer.lineRenderer.endColor;
        cEnd.a = 0.2f * power;
        renderer.lineRenderer.endColor = cEnd;
        //renderer.lineRenderer.colorGradient.alphaKeys[1].time = ((maxSignalRadius - dist) / maxSignalRadius);
    }

}
