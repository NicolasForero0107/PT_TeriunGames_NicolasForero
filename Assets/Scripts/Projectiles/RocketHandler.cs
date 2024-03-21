using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class RocketHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject explosionParticleSystemPrefab;

    [Header("Collision detection")]
    public Transform checkForImpactPoint;
    public LayerMask collisionLayers;

    //timing
    TickTimer maxLiveDurationTickTimer = TickTimer.None;

    //rocket info
    int rocketSpeed = 20;

    //hit info
    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    //fired info
    PlayerRef firedByPlayerRef;
    string firedByPlayerName;
    NetworkObject firedByNetworkObject;

    //other componetns
    NetworkObject networkObject;

    public void Fire(PlayerRef firedByPlayerRef, NetworkObject firedByNetworkObject, string firedByPlayerName)
    {
        this.firedByPlayerRef = firedByPlayerRef;
        this.firedByPlayerName = firedByPlayerName;
        this.firedByNetworkObject = firedByNetworkObject;

        networkObject = GetComponent<NetworkObject>();

        maxLiveDurationTickTimer = TickTimer.CreateFromSeconds(Runner, 10);
    }

    public override void FixedUpdateNetwork()
    {
        transform.position += transform.forward * Runner.DeltaTime * rocketSpeed;

        if (Object.HasStateAuthority)
        {
            //check if rocket has reached end of its life
            if (maxLiveDurationTickTimer.Expired(Runner))
            {
                Runner.Despawn(networkObject);

                return;
            }

            //check if rocket hit anything
            int hitCount = Runner.LagCompensation.OverlapSphere(checkForImpactPoint.position, 0.5f, firedByPlayerRef, hits, collisionLayers, HitOptions.IncludePhysX);

            bool isValidHit = false;

            //weve hit something so hit could be valid
            if (hitCount > 0)
                isValidHit = true;

            //check what weve hit
            for (int i = 0; i < hitCount; i++)
            {
                //check if we have hit a hitbox
                if (hits[i].Hitbox != null)
                {
                    //check that we didnt fire the rocket and hit ourselves. this can happen if the lag is a bit high
                    if (hits[i].Hitbox.Root.GetBehaviour<NetworkObject>() == firedByNetworkObject)
                        isValidHit = false;
                }
            }

            //we hit something valid
            if (isValidHit)
            {
                //deal damage to anything that was within blast radius
                hitCount = Runner.LagCompensation.OverlapSphere(checkForImpactPoint.position, 4, firedByPlayerRef, hits, collisionLayers, HitOptions.None);

                //deal damage to anything within hit radius
                for (int i = 0; i < hitCount; i++)
                {
                    HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();

                    if (hpHandler != null)
                        hpHandler.OnTakeDamage(firedByPlayerName, 100);
                }

                Runner.Despawn(networkObject);
            }
        }
    }

    //when despawning we create explosion
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Instantiate(explosionParticleSystemPrefab, checkForImpactPoint.position, Quaternion.identity);
    }
}
