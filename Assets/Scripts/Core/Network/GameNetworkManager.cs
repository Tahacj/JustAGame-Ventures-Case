using System;
using Mirror;
using UnityEngine;
using JustAGame.Inventory;

namespace JustAGame.Core.Network
{
    [AddComponentMenu("JustAGame/Game Network Manager")]
    public class GameNetworkManager : NetworkManager
    {
        [SerializeField] private Transform[] spawnPoints;

        private int _manualSpawnIndex = 0;

        // Cached host inventory for odd/even routing
        public static PlayerInventory HostInventory { get; private set; }
        public static event Action<PlayerInventory> OnHostInventoryAssigned;

        public override Transform GetStartPosition()
        {
            // Use manual spawn points if assigned; otherwise fallback to Mirror defaults
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var validPoints = Array.FindAll(spawnPoints, p => p != null);
                if (validPoints.Length > 0)
                {
                    if (playerSpawnMethod == PlayerSpawnMethod.Random)
                    {
                        return validPoints[UnityEngine.Random.Range(0, validPoints.Length)];
                    }

                    Transform point = validPoints[_manualSpawnIndex % validPoints.Length];
                    _manualSpawnIndex = (_manualSpawnIndex + 1) % validPoints.Length;
                    return point;
                }
            }

            return base.GetStartPosition();
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            // Register host inventory when local host player spawns
            if (conn == NetworkServer.localConnection && conn.identity != null)
            {
                var inventory = conn.identity.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    HostInventory = inventory;
                    OnHostInventoryAssigned?.Invoke(inventory);
                }
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn == NetworkServer.localConnection)
            {
                HostInventory = null;
            }

            base.OnServerDisconnect(conn);
        }

        public override void OnStopServer()
        {
            HostInventory = null;
            base.OnStopServer();
        }

        // Resolves host inventory reference on the server
        public static PlayerInventory GetHostInventory()
        {
            if (HostInventory != null) return HostInventory;

            if (NetworkServer.localConnection != null && NetworkServer.localConnection.identity != null)
            {
                HostInventory = NetworkServer.localConnection.identity.GetComponent<PlayerInventory>();
            }

            return HostInventory;
        }
    }
}
