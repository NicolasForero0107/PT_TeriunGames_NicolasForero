using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class WeaponHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GrenadeHandler grenadePrefab;
    public RocketHandler rocketPrefab;


    [Header("Effects")]
    public ParticleSystem fireParticleSystem;

    [Header("Aim")]
    public Transform aimPoint;

    [Header("Collision")]
    public LayerMask collisionLayers;

    [Networked(OnChanged = nameof(OnFireChanged))]
    public bool isFiring { get; set; }

    float lastTimeFired = 0;

    //timing
    TickTimer grenadeFireDelay = TickTimer.None;
    TickTimer rocketFireDelay = TickTimer.None;


    //other components
    HPHandler hpHandler;
    NetworkPlayer networkPlayer;
    NetworkObject networkObject;

    private void Awake()
    {
        hpHandler = GetComponent<HPHandler>();
        networkPlayer = GetBehaviour<NetworkPlayer>();
        networkObject = GetComponent<NetworkObject>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }


    public override void FixedUpdateNetwork()
    {
        if (hpHandler.isDead)
            return;

        //get input from network
        if(GetInput(out NetworkInputData networkInputData))
        {
            if (networkInputData.isFireButtonPressed)
                Fire(networkInputData.aimForwardVector);

            if (networkInputData.isGrenadeFireButtonPressed)
                FireGrenade(networkInputData.aimForwardVector);

            if (networkInputData.isRocketLauncherButtonPressed)
                FireRocket(networkInputData.aimForwardVector);
        }
    }

    void Fire(Vector3 aimForwardVector)
    {
        if (Time.time - lastTimeFired < 0.15f)
            return;

        StartCoroutine(FireEffectCO());

        Runner.LagCompensation.Raycast(aimPoint.position, aimForwardVector, 100, Object.InputAuthority, out var hitinfo, collisionLayers, HitOptions.IncludePhysX);

        float hitDistance = 100;
        bool isHitOtherPlayer = false;

        if (hitinfo.Distance > 0)
            hitDistance = hitinfo.Distance;

        if (hitinfo.Hitbox != null)
        {
            Debug.Log($"{Time.time} {transform.name} hit hitbox {hitinfo.Hitbox.transform.root.name}");

            if (Object.HasStateAuthority)
                hitinfo.Hitbox.transform.root.GetComponent<HPHandler>().OnTakeDamage(networkPlayer.nickName.ToString(), 1);

            isHitOtherPlayer = true;

        }
        else if (hitinfo.Collider != null)
        {
            Debug.Log($"{Time.time} {transform.name} hit physx {hitinfo.Collider.transform.root.name}");
        }

        //debug
        if (isHitOtherPlayer)
            Debug.DrawRay(aimPoint.position, aimForwardVector * hitDistance, Color.red, 1);
        else Debug.DrawRay(aimPoint.position, aimForwardVector * hitDistance, Color.green, 1);

        lastTimeFired = Time.time;
    }

    void FireGrenade(Vector3 aimForwardVector)
    {
        //check that we have not recently thrown grenade
        if (grenadeFireDelay.ExpiredOrNotRunning(Runner))
        {
            Runner.Spawn(grenadePrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) =>
            {
                spawnedGrenade.GetComponent<GrenadeHandler>().Throw(aimForwardVector * 10, Object.InputAuthority, networkPlayer.nickName.ToString());
            });

            //start a new timer to avoid spam
            grenadeFireDelay = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
    }

    void FireRocket(Vector3 aimForwardVector)
    {
        //check that we have not recently thrown grenade
        if (rocketFireDelay.ExpiredOrNotRunning(Runner))
        {
            Runner.Spawn(rocketPrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedRocket) =>
            {
                spawnedRocket.GetComponent<RocketHandler>().Fire(Object.InputAuthority, networkObject, networkPlayer.nickName.ToString());
            });

            //start a new timer to avoid spam
            rocketFireDelay = TickTimer.CreateFromSeconds(Runner, 4.0f);
        }
    }

    IEnumerator FireEffectCO()
    {
        isFiring = true;

        fireParticleSystem.Play();

        yield return new WaitForSeconds(0.09f);

        isFiring = false;
    }

    static void OnFireChanged(Changed<WeaponHandler> changed)
    {
        //Debug.Log($"{Time.time} OnFireChanged value {changed.Behaviour.isFiring}");

        bool isFiringCurrent = changed.Behaviour.isFiring;

        //load the old value
        changed.LoadOld();

        bool isFiringOld = changed.Behaviour.isFiring;

        if (isFiringCurrent && !isFiringOld)
            changed.Behaviour.OnFireRemote();
    }

    void OnFireRemote()
    {
        if (!Object.HasInputAuthority)
            fireParticleSystem.Play();
    }
}
