using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;

public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkPlayer playerPrefab;

    //Mapping between token ID and re-created players
    Dictionary<int, NetworkPlayer> mapTokenIDWithNetworkPlayer;

    //other components
    CharacterInputHandler characterInputHandler;

    void Awake()
    {
        //create a new dictionary
        mapTokenIDWithNetworkPlayer = new Dictionary<int, NetworkPlayer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    int GetPlayerToken(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer == player)
        {
            //just use the local player connection token
            return ConnectionTokenUtils.HashToken(GameManager.instance.GetConnectionToken());
        }
        else
        {
            //get the connection token stored when the client connects to this host
            var token = runner.GetPlayerConnectionToken(player);

            if (token != null)
                return ConnectionTokenUtils.HashToken(token);

            Debug.LogError($"GetPlayerToken returned invalid token");

            return 0; //invalid
        }
    }

    public void SetConnectionTokenMapping(int token, NetworkPlayer networkPlayer)
    {
        mapTokenIDWithNetworkPlayer.Add(token, networkPlayer);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            //get token for the player
            int playerToken = GetPlayerToken(runner, player);

            Debug.Log($"OnPlayerJoined we are server. Connection token {playerToken}");

            //check if the token is already recorded by the server
            if (mapTokenIDWithNetworkPlayer.TryGetValue(playerToken, out NetworkPlayer networkPlayer))
            {
                Debug.Log($"Found old connection token for token {playerToken}. Assigning controls to that player");

                networkPlayer.GetComponent<NetworkObject>().AssignInputAuthority(player);

                networkPlayer.Spawned();
            }
            else
            {
                Debug.Log($"Spawning new player for connection token {playerToken}");
                NetworkPlayer spawnedNetworkPlayer = runner.Spawn(playerPrefab, Utils.GetRandomSpawnPoint(), Quaternion.identity, player);

                //store thje token for the player
                spawnedNetworkPlayer.token = playerToken;

                //store mapping between playerToken and the spawned network player
                mapTokenIDWithNetworkPlayer[playerToken] = spawnedNetworkPlayer;
            }

        }
        else
            Debug.Log("OnPlayerJoined");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (characterInputHandler == null && NetworkPlayer.local != null)
            characterInputHandler = NetworkPlayer.local.GetComponent<CharacterInputHandler>();

        if (characterInputHandler != null)
            input.Set(characterInputHandler.GetNetworkInput());
    }

    public void OnConnectedToServer(NetworkRunner runner){ Debug.Log("OnConnectedToServer"); }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { Debug.Log("OnShutDown"); }
    public void OnDisconnectedFromServer(NetworkRunner runner) { Debug.Log("OnDisconnectedFromServer"); }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { Debug.Log("OnConnectRequest"); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAdress, NetConnectFailedReason reason) { Debug.Log("OnConnectFailed"); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) 
    {
        Debug.Log("OnHostMigration");

        //shut down current runnet
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        //find the network runner handler and start the host migration
        FindObjectOfType<NetworkRunnerHandler>().StartHostMigration(hostMigrationToken);
    }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnHostMigrationCleanup()
    {
        Debug.Log("Spawner OnHostMigrationCleanup started");

        foreach(KeyValuePair<int, NetworkPlayer> entry in mapTokenIDWithNetworkPlayer)
        {
            NetworkObject networkObjectInDictionary = entry.Value.GetComponent<NetworkObject>();

            if (networkObjectInDictionary.InputAuthority.IsNone)
            {
                Debug.Log($"{Time.time} found player that has not reconnected. Despawning {entry.Value.nickName}");

                networkObjectInDictionary.Runner.Despawn(networkObjectInDictionary);
            }
        }

        Debug.Log("Spawner OnHostMigrationCleanup completed");
    }
}
