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
    public bool isUnblockableSignal;
    public float signalSourceSize = 0.2f;
    //TODO Add visual indicator for signal radius, semi transparent circle?

    [SerializeField]
    private LayerMask signalSearchMask;
    [SerializeField]
    private LayerMask signalBlockMask;

    public float signalLossPerUnitDistance = 0.1f;
    public float minDistanceToReceiveMaxSignal = 1.25f;

    //increase this when it inevitably is not enough
    Collider[] cols = new Collider[32];

    // Update is called once per frame
    //Why is this not on fixed update? im changing it
    //Allowing multiple signal receivers so we can power up enemies and obstacles too etc
    private HashSet<SignalReceptor> receivers = new HashSet<SignalReceptor>();
    private Dictionary<SignalReceptor, SignalRenderer> signalRenderers = new Dictionary<SignalReceptor, SignalRenderer>();
    private List<SignalReceptor> receiversToRemove = new List<SignalReceptor>();
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
    private void Awake()
    {
        GameManager.OnLevelClear += OnLevelClear;
    }
    void FixedUpdate()
    {
        if (GameManager.Instance.isTransitioning)
            return;

        receivers.Clear();

        Physics.OverlapSphereNonAlloc(transform.position, maxSignalRadius, cols, signalSearchMask);
        foreach (Collider col in cols)
        {
            if (!col)
                continue;

            //only supporting one signal receiver per collider, not sure if need more?
            if (col.TryGetComponent<SignalReceptor>(out SignalReceptor signalReceiver))
            {
                if (isUnblockableSignal)
                    receivers.Add(signalReceiver);
                else if (!Physics.Linecast(transform.position, signalReceiver.transform.position, signalBlockMask))
                {
                    receivers.Add(signalReceiver);
                }
            }

            else if (col.gameObject.layer == LayerMask.NameToLayer("Reflector"))
            {
                // TODO: does it have a clear path to reflector and then reflectors bounce hits the player?
            }
        }
        foreach (var signalReceiver in receivers)
        {
            signalReceiver.AddSignal(this);
            if (!signalRenderers.ContainsKey(signalReceiver))
            {
                SignalRenderer newRenderer = PoolManager.DequeueObject<SignalRenderer>(PoolerType.SIGNAL_RENDERER);
                //newRenderer.transform.SetParent(signalRendererHolder.transform, false);
                //  newRenderer.transform.localPosition = Vector3.zero;
                newRenderer.gameObject.SetActive(true);
                signalRenderers.Add(signalReceiver, newRenderer);
            }
        }
        receiversToRemove.Clear();
        foreach (KeyValuePair<SignalReceptor, SignalRenderer> signalRender in signalRenderers)
        {
            if (!receivers.Contains(signalRender.Key))
            {
                receiversToRemove.Add(signalRender.Key);
            }
        }
        foreach (SignalReceptor signalReceiver in receiversToRemove)
        {
            signalRenderers[signalReceiver].gameObject.SetActive(false);
            PoolManager.EnqueueObject(signalRenderers[signalReceiver], PoolerType.SIGNAL_RENDERER);
            signalRenderers.Remove(signalReceiver);
        }
    }

    //Todo, change color based on signal receiver type?
    public void UpdateSignalVisualization(SignalReceptor receiver, Transform signalReceiver, float dist)
    {
        if (!signalRenderers.ContainsKey(receiver)) return;

        var renderer = signalRenderers[receiver];

        renderer.lineRenderer.SetPosition(0, transform.position);

        float power = ((maxSignalRadius - dist) / maxSignalRadius);
        renderer.lineRenderer.SetPosition(1, signalReceiver.position);

        // Color cStart = renderer.lineRenderer.startColor;
        Color cStart = receiver.startColor;

        cStart.a = 0.6f * power;
        renderer.lineRenderer.startColor = cStart;

        // Color cEnd = renderer.lineRenderer.endColor;
        Color cEnd = receiver.endColor;

        cEnd.a = 0.2f * power;
        renderer.lineRenderer.endColor = cEnd;
        //renderer.lineRenderer.colorGradient.alphaKeys[1].time = ((maxSignalRadius - dist) / maxSignalRadius);
    }
    private void OnLevelClear()
    {
        GameManager.OnLevelClear -= OnLevelClear;
        foreach (SignalRenderer signalRenderer in signalRenderers.Values)
        {
            if (signalRenderer == null) continue; //idk

            signalRenderer.gameObject.SetActive(false);
            PoolManager.EnqueueObject(signalRenderer, PoolerType.SIGNAL_RENDERER);
        }
        signalRenderers.Clear();

        Destroy(this);
    }
}
