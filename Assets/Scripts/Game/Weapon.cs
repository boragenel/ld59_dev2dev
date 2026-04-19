using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [Header("Base Attributes")]
    public float noSignalCooldown = 2f;
    public float baseAttackCooldown = 0.5f;
    public float maxAttackCooldown = 0.1f;
    public float baseReloadSpeed = 0.2f;
    public int baseMagazineSize = 8;

    [Header("Dynamics")]
    private float attackCooldownTimer = 0f;
    private float reloadProgress = 0f;
    private bool isReloading = false;
    private bool isTryingToFire = false;
    private int noOfBulletsLeft = 8;

    public ParticleSystem PS;
    public Transform bulletOrigin;

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
        attackCooldownTimer += Time.deltaTime;

        //Trying auto fire to see if it feels good comment this out to go back to hold click to shoot;
        if (attackCooldownTimer >= GetAttackCooldown())
        {
            Fire();
        }
        return;
        ////////

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!isReloading && (attackCooldownTimer >= GetAttackCooldown()))
            {
                Fire();
            }
        }
        isTryingToFire = Mouse.current.leftButton.isPressed;
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

        if (isTryingToFire)
        {
            if (attackCooldownTimer >= GetAttackCooldown())
            {
                Fire();
            }
        }
    }

    void Fire()
    {
        //this was feeling really punishing and not great, with the updated way the firerate + signal work just a long cooldown should be fine
        //if (signalReceptor.ReceptionStrenght == 0)
        //    return;

        //Reloading + the reception mechanic was not feeling great
        //noOfBulletsLeft--;
        //if (noOfBulletsLeft <= 0)
        //{
        //    reloadProgress = 0;
        //    isReloading = true;
        //}

        PS?.Play();
        Projectile pBullet = PoolManager.DequeueObject<Projectile>(PoolerType.PLAYER_BULLET);
        pBullet.gameObject.SetActive(true);
        pBullet.transform.position = bulletOrigin.position;
        pBullet.transform.up = bulletOrigin.up;
        attackCooldownTimer = 0;

        //Debug.Log("Yeah");
    }

    public float GetAttackCooldown()
    {
        if (signalReceptor.ReceptionStrenght <= 0) return noSignalCooldown;

        return Mathf.Lerp(baseAttackCooldown, maxAttackCooldown, signalReceptor.ReceptionStrenght);

        // return Mathf.Clamp(baseAttackCooldown / signalReceptor.ReceptionStrenght, 0.1f, 1f);
    }

    public float GetReloadSpeed()
    {
        if (signalReceptor.ReceptionStrenght == 0)
            return 0;
        return baseReloadSpeed / signalReceptor.ReceptionStrenght;
    }
}
