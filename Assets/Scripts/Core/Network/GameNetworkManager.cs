using System;
using Mirror;
using UnityEngine;
using JustAGame.Inventory;

namespace JustAGame.Core.Network
{
    /// <summary>
    /// Custom NetworkManager managing player life cycles and maintaining a reference 
    /// to the Host's PlayerInventory for server-authoritative vending machine distribution.
    /// </summary>
    [AddComponentMenu("JustAGame/Game Network Manager")]
    public class GameNetworkManager : NetworkManager
    {
        /// <summary>
        /// Reference to the host player's inventory (only valid on the server/host).
        /// </summary>
        public static PlayerInventory HostInventory { get; private set; }

        public static event Action<PlayerInventory> OnHostInventoryAssigned;

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            // Check if this newly spawned player belongs to the local Host player
            if (conn == NetworkServer.localConnection && conn.identity != null)
            {
                var inventory = conn.identity.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    HostInventory = inventory;
                    OnHostInventoryAssigned?.Invoke(inventory);
                    Debug.Log("[GameNetworkManager] Host PlayerInventory successfully registered.");
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

        /// <summary>
        /// Utility helper to find the host player's inventory on the server if not cached.
        /// </summary>
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
