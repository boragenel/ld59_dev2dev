using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class SignalMeshPointReceiver : MonoBehaviour {
    [SerializeField]
    private Vector3 worldOffset;

    public bool preciseSignalStrength = false;
    //private List<SignalRenderer> signalRenderers = new List<SignalRenderer>();
    public float SignalStrength;
    public bool affectMusic = false;
    public bool isOn = true;
    public Animator animator;

    public event Action<int> OnSignalStrengthChanged;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
    private void LateUpdate()
    {
        Vector3 sample = transform.position + worldOffset;
        SignalMeshFieldManager m = SignalMeshFieldManager.Instance;


        int next = m != null ? m.CountMeshesContainingWorldPoint(sample) : 0;
        if (next == SignalStrength && !preciseSignalStrength)
        {
            return;
        }

        int outcome = 0;
        if (!preciseSignalStrength)
        {
            SignalStrength = next;
            outcome = Mathf.RoundToInt(SignalStrength);
        }
        else
        {
            SignalStrength = 0;
            List<Vector3> poses = m != null ? m.GetMeshesContainingWorldPoint(sample) : new List<Vector3>();
            foreach (Vector3 pos in poses)
            {
                float diff = Mathf.Clamp((pos - transform.position).magnitude, 0.25f, 8f);

                SignalStrength += ((8f - diff) / 8f);
            }
            outcome = Mathf.RoundToInt(SignalStrength * 10);

            if (affectMusic)
            {
                SoundManager.Instance.masterMixer.GetFloat("MusicLowpass", out float lowpass);
                SoundManager.Instance.masterMixer.SetFloat("MusicLowpass", Mathf.Lerp(lowpass, 200 + 1000 * SignalStrength, Time.deltaTime * 10f));
                GameManager.Instance.uiManager.UpdateReceptionVisuals(SignalStrength);
            }
        }

        OnSignalStrengthChanged?.Invoke(outcome);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (SignalStrength >= 1f)
        {
            if (!isOn)
            {
                isOn = true;
                if (animator != null)
                    animator.SetBool("IsOn", true);
                Debug.Log("Turned on");
            }
        }
        else
        {
            if (isOn)
            {
                isOn = false;
                if (animator != null)
                    animator.SetBool("IsOn", false);
                Debug.Log("Turned off");
            }
        }
    }

    public void ResetSources() {
    }
}
