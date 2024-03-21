using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GrenadeHandler : NetworkBehaviour
{
    public GameObject explosionParticleSystemPrefab;

    //thrown by info
    PlayerRef thrownByPlayerRef;
    string thrownByPlayerName;

    //timing
    TickTimer explodeTickTimer = TickTimer.None;

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
