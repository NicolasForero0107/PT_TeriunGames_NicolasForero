using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkInGameMessages : NetworkBehaviour
{
    InGameMessagesUIHandler inGameMessagesUIHandler;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SendInGameRPCMessage(string userNickName, string message)
    {
        RPC_InGameMessage($"<b>{userNickName}</b> {message}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_InGameMessage(string message, RpcInfo info = default)
    {
        Debug.Log($"[RPC] InGameMessage {message}");

        if (inGameMessagesUIHandler == null)
            inGameMessagesUIHandler = NetworkPlayer.local.localCameraHandler.GetComponentInChildren<InGameMessagesUIHandler>();

        if (inGameMessagesUIHandler != null)
            inGameMessagesUIHandler.OnGameMessageReceived(message);
    }
}
