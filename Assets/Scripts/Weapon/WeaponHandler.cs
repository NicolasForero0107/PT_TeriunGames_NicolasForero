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

    float maxHitDistance = 200;

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
                Fire(networkInputData.aimForwardVector, networkInputData.cameraPosition);

            if (networkInputData.isGrenadeFireButtonPressed)
                FireGrenade(networkInputData.aimForwardVector);

            if (networkInputData.isRocketLauncherButtonPressed)
                FireRocket(networkInputData.aimForwardVector);
        }
    }

    void Fire(Vector3 aimForwardVector, Vector3 cameraPosition)
    {
        if (Time.time - lastTimeFired < 0.15f)
            return;

        StartCoroutine(FireEffectCO());

        LagCompensatedHit hitinfo = new LagCompensatedHit();

        Vector3 fireDirection = aimForwardVector;
        float hitDistance = maxHitDistance;

        bool isHitOtherPlayer = false;

        if (networkPlayer.is3rdPersonCamera)
        {
            Runner.LagCompensation.Raycast(cameraPosition, fireDirection, hitDistance, Object.InputAuthority, out hitinfo, collisionLayers, HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority);

            //check against other players
            if (hitinfo.Hitbox != null)
            {
                fireDirection = (hitinfo.Point - aimPoint.position).normalized;
                hitDistance = hitinfo.Distance;

                if (Object.HasStateAuthority)
                {
                    hitinfo.Hitbox.transform.root.GetComponent<HPHandler>().OnTakeDamage(networkPlayer.nickName.ToString(), 1);
                }

                isHitOtherPlayer = true;
                Debug.DrawRay(cameraPosition, aimForwardVector * hitDistance, new Color(0.4f, 0, 0), 1);
            }
            else if(hitinfo.Collider != null)
            {
                fireDirection = (hitinfo.Point - aimPoint.position).normalized;
                hitDistance = hitinfo.Distance;

                Debug.DrawRay(cameraPosition, aimForwardVector * hitDistance, new Color(0, 0.4f, 0), 1);
            }
            else
            {
                Debug.DrawRay(cameraPosition, fireDirection * hitDistance, Color.gray, 1);

                fireDirection = ((cameraPosition + fireDirection * hitDistance) - aimPoint.position).normalized;
            }
        }

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
