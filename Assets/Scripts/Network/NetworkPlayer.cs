using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer local { get; set; }

    //public Transform playerModel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            local = this;
            //GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>().Priority = 10;
            Debug.Log("Spawned local player");
        }
        else
        {
            //GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>().Priority = 0;
            //Cinemachine.CinemachineVirtualCamera virtualCamera = GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>();
            //virtualCamera.enabled = false;
            Camera localCamera = GetComponentInChildren<Camera>();
            localCamera.enabled = false;

            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;
            Debug.Log("Spawned remote player");
        }

        //make it easier to tell which player is which
        transform.name = $"P_{Object.Id}";
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }

}
