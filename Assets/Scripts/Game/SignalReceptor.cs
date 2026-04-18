using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//cant name it signal receiver lol thats a unity class
public class SignalReceptor : MonoBehaviour
{
    private List<SignalSource> signalSources = new List<SignalSource>();
    private List<SignalSource> expectedSignalLoss = new List<SignalSource>();

    [ReadOnlyAttribute] private float receptionStrenght = 0f;
    private float prevReception = 0f;

    public static UnityAction<float> OnReceptionChanged;

    public float ReceptionStrenght { get => receptionStrenght; set => receptionStrenght = value; }

    void Update()
    {
        UpdateReception();
    }

    public void AddSignal(SignalSource inSignalSource)
    {
        if (!signalSources.Contains(inSignalSource))
        {
            signalSources.Add(inSignalSource);
        }
    }
    void UpdateReception()
    {
        ReceptionStrenght = 0;
        signalSources.RemoveAll(item => item == null);
        foreach (SignalSource signalSource in signalSources)
        {
            Vector3 diff = signalSource.transform.position - transform.position;
            float dist = diff.magnitude;
            float signalGain = 1 - ((dist) / (signalSource.maxSignalRadius));

            if (dist > signalSource.maxSignalRadius)
            {
                expectedSignalLoss.Add(signalSource);
            }
            else
            {
                signalSource.UpdateSignalVisualization(this, transform, dist);
                ReceptionStrenght += signalGain;
            }
        }

        if (prevReception != ReceptionStrenght)
        {
            prevReception = ReceptionStrenght;
            OnReceptionChanged?.Invoke(ReceptionStrenght);
        }

        if (expectedSignalLoss.Count > 0)
        {
            for (int i = 0; i < expectedSignalLoss.Count; i++)
            {
                signalSources.Remove(expectedSignalLoss[i]);
            }
            // TODO: play signal lost animation for this source
            expectedSignalLoss.Clear();
        }
    }
    public void ResetSources()
    {
        signalSources.Clear();
    }
    public float GetReception()
    {
        return ReceptionStrenght;
    }
}
