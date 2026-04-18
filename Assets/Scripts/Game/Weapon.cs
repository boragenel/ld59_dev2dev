using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
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

    //public static UnityAction<float> OnReceptionChanged;

    public SignalReceptor signalReceptor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        noOfBulletsLeft = baseMagazineSize;
    }

    // Update is called once per frame
    void Update()
    {
        HandleFiring();
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

        //Reloading + the reception mechanic was not feeling great
        //if (isReloading)
        //{
        //    reloadProgress += Time.deltaTime * GetReloadSpeed();
        //    if (reloadProgress >= 1f)
        //    {
        //        isReloading = false;
        //        noOfBulletsLeft = baseMagazineSize;
        //    }
        //}

        else if (isTryingToFire)
        {
            if (attackCooldownTimer <= 0)
            {
                Fire();
            }
        }
    }

    void Fire()
    {
        if (signalReceptor.ReceptionStrenght == 0)
            return;

        //Reloading + the reception mechanic was not feeling great
        //noOfBulletsLeft--;
        //if (noOfBulletsLeft <= 0)
        //{
        //    reloadProgress = 0;
        //    isReloading = true;
        //}

        Projectile pBullet = PoolManager.DequeueObject<Projectile>(PoolerType.PLAYER_BULLET);
        pBullet.gameObject.SetActive(true);
        pBullet.transform.position = transform.position;
        pBullet.transform.up = transform.up;
        attackCooldownTimer = GetAttackCooldown();

        Debug.Log("Yeah");
    }

    public float GetAttackCooldown()
    {
        if (signalReceptor.ReceptionStrenght == 0)
            return 3f;
        return Mathf.Clamp(baseAttackCooldown / signalReceptor.ReceptionStrenght, 0.1f, 1f);
    }

    public float GetReloadSpeed()
    {
        if (signalReceptor.ReceptionStrenght == 0)
            return 0;
        return baseReloadSpeed / signalReceptor.ReceptionStrenght;
    }
}
