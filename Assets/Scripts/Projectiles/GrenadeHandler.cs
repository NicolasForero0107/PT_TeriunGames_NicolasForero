using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GrenadeHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject explosionParticleSystemPrefab;

    [Header("Collision Detection")]
    public LayerMask collisionLayers;

    //thrown by info
    PlayerRef thrownByPlayerRef;
    string thrownByPlayerName;

    //timing
    TickTimer explodeTickTimer = TickTimer.None;

    //hitinfo
    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    //other components
    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;
    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, string thrownByPlayerName)
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();

        networkRigidbody.Rigidbody.AddForce(throwForce, ForceMode.Impulse);

        this.thrownByPlayerRef = thrownByPlayerRef;
        this.thrownByPlayerName = thrownByPlayerName;

        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 2);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (explodeTickTimer.Expired(Runner))
            {
                int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, 4, thrownByPlayerRef, hits, collisionLayers);

                for(int i = 0; i < hitCount; i++)
                {
                    HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();

                    if (hpHandler != null)
                        hpHandler.OnTakeDamage(thrownByPlayerName, 3);
                }

                Runner.Despawn(networkObject);

                //stop explode timer from being triggered
                explodeTickTimer = TickTimer.None;
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>();

        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity);
    }
}
