using System;
using System.Collections.Generic;
using Mirror;
using JustAGame.Core.Network;
using JustAGame.Pooling;
using UnityEngine;

namespace JustAGame.Core.Platform
{
    /// <summary>
    /// Production implementation of IPlatformNetworkService.
    /// Manages Mirror P2P session lifecycles, host initiation, client connections, and peer authorization.
    /// </summary>
    public sealed class EOSPlatformNetworkService : IPlatformNetworkService
    {
        public bool IsHost => NetworkServer.active && NetworkClient.isConnected;
        public bool IsClient => NetworkClient.isConnected && !NetworkServer.active;
        public bool IsSessionActive => NetworkServer.active || NetworkClient.active;

        public string HostAddress
        {
            get
            {
                if (EOSNetworkManagerBridge.Instance.IsNotNull())
                {
                    return EOSNetworkManagerBridge.Instance.LocalProductId;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public int ConnectedPeerCount
        {
            get
            {
                if (NetworkServer.active)
                {
                    return NetworkServer.connections.Count;
                }
                else
                {
                    return NetworkClient.isConnected ? 1 : 0;
                }
            }
        }

        public bool UseActiveWhitelist
        {
            get
            {
                if (EOSNetworkManagerBridge.Instance.IsNotNull())
                {
                    return EOSNetworkManagerBridge.Instance.UseActiveWhitelist;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (EOSNetworkManagerBridge.Instance.IsNotNull())
                {
                    EOSNetworkManagerBridge.Instance.UseActiveWhitelist = value;
                }
                else
                {
                    // Bridge not yet initialized
                }
            }
        }

        public event Action<string> OnHostStarted;
        public event Action<string> OnClientStarted;
        public event Action OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public event Action OnSessionStopped;
        public event Action<string> OnNetworkError;

        public EOSPlatformNetworkService()
        {
            try
            {
                EOSNetworkManagerBridge.OnEosHostStarted += HandleHostStarted;
                EOSNetworkManagerBridge.OnEosClientStarted += HandleClientStarted;
                EOSNetworkManagerBridge.OnEosSessionStopped += HandleSessionStopped;
                EOSNetworkManagerBridge.OnEosError += HandleNetworkError;

                GameNetworkManager.OnClientConnectedToServer += HandleClientConnected;
                GameNetworkManager.OnClientDisconnectedFromServer += HandleClientDisconnected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in constructor: {ex.Message}");
            }
        }

        public bool StartHost()
        {
            try
            {
                var bridge = GetBridge();
                if (bridge.IsNull())
                {
                    string error = "EOSNetworkManagerBridge instance not found in scene.";
                    Debug.LogError($"[EOSPlatformNetworkService] {error}");
                    OnNetworkError?.Invoke(error);
                    return false;
                }
                else
                {
                    return bridge.StartEosHost();
                }
            }
            catch (Exception ex)
            {
                string error = $"Exception in StartHost: {ex.Message}";
                Debug.LogError($"[EOSPlatformNetworkService] {error}");
                OnNetworkError?.Invoke(error);
                return false;
            }
        }

        public bool JoinHost(string hostProductUserId)
        {
            try
            {
                var bridge = GetBridge();
                if (bridge.IsNull())
                {
                    string error = "EOSNetworkManagerBridge instance not found in scene.";
                    Debug.LogError($"[EOSPlatformNetworkService] {error}");
                    OnNetworkError?.Invoke(error);
                    return false;
                }
                else
                {
                    return bridge.StartEosClient(hostProductUserId);
                }
            }
            catch (Exception ex)
            {
                string error = $"Exception in JoinHost: {ex.Message}";
                Debug.LogError($"[EOSPlatformNetworkService] {error}");
                OnNetworkError?.Invoke(error);
                return false;
            }
        }

        public void StopSession()
        {
            try
            {
                var bridge = GetBridge();
                if (bridge.IsNotNull())
                {
                    bridge.StopSession();
                }
                else
                {
                    if (NetworkServer.active && NetworkClient.isConnected)
                    {
                        NetworkManager.singleton.StopHost();
                    }
                    else if (NetworkClient.isConnected || NetworkClient.active)
                    {
                        NetworkManager.singleton.StopClient();
                    }
                    else if (NetworkServer.active)
                    {
                        NetworkManager.singleton.StopServer();
                    }
                    else
                    {
                        // No active session
                    }

                    OnSessionStopped?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in StopSession: {ex.Message}");
            }
        }

        public void AddAuthorizedPeer(string productUserId)
        {
            try
            {
                var bridge = GetBridge();
                if (bridge.IsNotNull())
                {
                    bridge.AddAuthorizedPeer(productUserId);
                }
                else
                {
                    // Bridge not available
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in AddAuthorizedPeer: {ex.Message}");
            }
        }

        public void RemoveAuthorizedPeer(string productUserId)
        {
            try
            {
                var bridge = GetBridge();
                if (bridge.IsNotNull())
                {
                    bridge.RemoveAuthorizedPeer(productUserId);
                }
                else
                {
                    // Bridge not available
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in RemoveAuthorizedPeer: {ex.Message}");
            }
        }

        public bool IsPeerAuthorized(string productUserId)
        {
            try
            {
                var bridge = GetBridge();
                if (bridge.IsNotNull())
                {
                    return bridge.IsPeerAuthorized(productUserId);
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in IsPeerAuthorized: {ex.Message}");
                return false;
            }
        }

        public void ClearAuthorizedPeers()
        {
            try
            {
                // Cleared via disable or custom bridge logic
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in ClearAuthorizedPeers: {ex.Message}");
            }
        }

        public bool CopyHostAddressToClipboard()
        {
            try
            {
                var bridge = GetBridge();
                if (bridge.IsNotNull())
                {
                    return bridge.CopyHostAddressToClipboard();
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in CopyHostAddressToClipboard: {ex.Message}");
                return false;
            }
        }

        private EOSNetworkManagerBridge GetBridge()
        {
            try
            {
                if (EOSNetworkManagerBridge.Instance.IsNotNull())
                {
                    return EOSNetworkManagerBridge.Instance;
                }
                else
                {
                    return UnityEngine.Object.FindFirstObjectByType<EOSNetworkManagerBridge>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in GetBridge: {ex.Message}");
                return null;
            }
        }

        private void HandleHostStarted(string localPuid)
        {
            try
            {
                OnHostStarted?.Invoke(localPuid);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in HandleHostStarted: {ex.Message}");
            }
        }

        private void HandleClientStarted(string targetHost)
        {
            try
            {
                OnClientStarted?.Invoke(targetHost);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in HandleClientStarted: {ex.Message}");
            }
        }

        private void HandleSessionStopped()
        {
            try
            {
                OnSessionStopped?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in HandleSessionStopped: {ex.Message}");
            }
        }

        private void HandleNetworkError(string error)
        {
            try
            {
                OnNetworkError?.Invoke(error);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in HandleNetworkError: {ex.Message}");
            }
        }

        private void HandleClientConnected()
        {
            try
            {
                OnClientConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in HandleClientConnected: {ex.Message}");
            }
        }

        private void HandleClientDisconnected()
        {
            try
            {
                OnClientDisconnected?.Invoke("Disconnected from remote host.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlatformNetworkService] Exception in HandleClientDisconnected: {ex.Message}");
            }
        }
    }
}
