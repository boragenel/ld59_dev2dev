using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [Header("Signal")]
    [SerializeField]
    [ReadOnlyAttribute]
    private float reception = 0f;

    private float prevReception = 0f;

    
    
    [Header("Base Attributes")]
    public float baseAttackCooldown = 0.5f;
    public float baseReloadSpeed = 0.2f;
    public int baseMagazineSize = 8;

    [Header("Dynamics")] 
    private float attackCooldownTimer = 0f;
    private float reloadProgress = 0f;
    private bool isReloading = false;
    private bool isTryingToFire = false;
    private int noOfBulletsLeft = 8;
    
    
    
    
    private List<SignalSource> signalSources = new List<SignalSource>();
    private List<SignalSource> expectedSignalLoss = new List<SignalSource>();


    public static UnityAction<float> OnReceptionChanged;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        noOfBulletsLeft = baseMagazineSize;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateReception();
        HandleFiring();
    }

    public void ResetSources()
    {
        signalSources.Clear();
    }
    
    void HandleFiring()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!isReloading && attackCooldownTimer <= 0)
            {
                Fire();
            }
        }
        isTryingToFire = Mouse.current.leftButton.isPressed;

        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
            
        }
        
        if (isReloading)
        {
            reloadProgress += Time.deltaTime * GetReloadSpeed();
            if (reloadProgress >= 1f)
            {
                isReloading = false;
                noOfBulletsLeft = baseMagazineSize;
            }
        }
        else if(isTryingToFire)
        {
            if (attackCooldownTimer <= 0)
            {
                Fire();
            }
        }

        

    }

    void Fire()
    {
        if (reception == 0)
            return;
        
        noOfBulletsLeft--;
        if (noOfBulletsLeft <= 0)
        {
            reloadProgress = 0;
            isReloading = true;
        }

        Projectile pBullet = PoolManager.DequeueObject<Projectile>(PoolerType.PLAYER_BULLET);
        pBullet.gameObject.SetActive(true);
        pBullet.transform.position = transform.position;
        pBullet.transform.up = transform.up;
        attackCooldownTimer = GetAttackCooldown();

        Debug.Log("Yeah");
    }
    
    public float GetAttackCooldown()
    {
        if(reception == 0)
            return 3f;
        return Mathf.Clamp(baseAttackCooldown / reception,0.1f,1f);
    }

    public float GetReloadSpeed()
    {
        if (reception == 0)
            return 0;
        return baseReloadSpeed / reception;
    }
    
    void UpdateReception()
    {
        reception = 0;
        foreach (SignalSource signalSource in signalSources)
        {
            Vector3 diff = signalSource.transform.position - transform.position;
            float dist = diff.magnitude;
            float signalGain = 1- ((dist) /(signalSource.maxSignalRadius));

            if (dist > signalSource.maxSignalRadius)
            {
                expectedSignalLoss.Add(signalSource);
            }
            else
            {
                signalSource.UpdateSignalVisualization(transform,dist);
                reception += signalGain;    
            }
        }

        if (prevReception != reception)
        {
            prevReception = reception;
            OnReceptionChanged?.Invoke(reception);
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
    
    public float GetReception()
    {
        return reception;
    }

    public void AddSignal(SignalSource inSignalSource)
    {
        if (!signalSources.Contains(inSignalSource))
        {
            signalSources.Add(inSignalSource);
        }
    }
    
    
}
