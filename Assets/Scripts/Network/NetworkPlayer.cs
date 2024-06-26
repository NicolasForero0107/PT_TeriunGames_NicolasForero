using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public TextMeshProUGUI playerNickNameTM;
    public static NetworkPlayer local { get; set; }

    public Transform playerModel;

    [Networked(OnChanged = nameof(OnNickNameChanged))]
    public NetworkString<_16> nickName { get; set; }

    //remote client token hash
    [Networked] public int token { get; set; }

    bool isPublicJoinMessageSent = false;

    public LocalCameraHandler localCameraHandler;
    public GameObject localUI;

    //camera mode
    public bool is3rdPersonCamera = true;

    //other components
    NetworkInGameMessages networkInGameMessages;

    private void Awake()
    {
        networkInGameMessages = GetComponent<NetworkInGameMessages>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            local = this;

            //disable main camera
            if (Camera.main != null)
                Camera.main.gameObject.SetActive(false);

            //enable 1 audio listener
            AudioListener audioListener = GetComponentInChildren<AudioListener>(true);
            audioListener.enabled = true;

            //enable the local camera
            localCameraHandler.localCamera.enabled = true;

            //detach camera if enabled
            localCameraHandler.transform.parent = null;

            //enable ui for local plasyer
            localUI.SetActive(true);

            RPC_SetNickName(GameManager.instance.playerNickName);

            RPC_CameraMode(is3rdPersonCamera);

            Debug.Log("Spawned local player");
        }
        else
        {
            //disable local camera for remote players
            localCameraHandler.localCamera.enabled = false;

            //disable ui for remote player
            localUI.SetActive(false);

            //only 1 audio listener per scene, disable remote players' audio listener
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;

            Debug.Log("Spawned remote player");
        }

        //set the player as a player object
        Runner.SetPlayerObject(Object.InputAuthority, Object);

        //make it easier to tell which player is which
        transform.name = $"P_{Object.Id}";
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Object.HasStateAuthority)
        {
            if(Runner.TryGetPlayerObject(player, out NetworkObject playerLeftNetworkObject))
            {
                if (playerLeftNetworkObject == Object)
                    local.GetComponent<NetworkInGameMessages>().SendInGameRPCMessage(playerLeftNetworkObject.GetComponent<NetworkPlayer>().nickName.ToString(), "left");
            }

        }

        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }

    static void OnNickNameChanged(Changed<NetworkPlayer> changed)
    {
        Debug.Log($"{Time.time} OnNickNameChanged name {changed.Behaviour.nickName}");

        changed.Behaviour.OnNickNameChanged();
    }

    private void OnNickNameChanged()
    {
        Debug.Log($"Nickname changed for player to {nickName} for player {gameObject.name}");

        playerNickNameTM.text = nickName.ToString();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"[RPC] SetNickName {nickName}");
        this.nickName = nickName;

        if (!isPublicJoinMessageSent)
        {
            networkInGameMessages.SendInGameRPCMessage(nickName, "joined");

            isPublicJoinMessageSent = true;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_CameraMode(bool is3rdPersonCamera, RpcInfo info = default)
    {
        Debug.Log($"[RPC] SetCameraMode, is3rdPersonCamera {is3rdPersonCamera}");

        this.is3rdPersonCamera = is3rdPersonCamera;
    }

    void OnDestroy()
    {
        //get rid of local camera if we get destroyed as a new one will be spawned with the new network player
        if (localCameraHandler != null)
            Destroy(localCameraHandler.gameObject);
    }
}
