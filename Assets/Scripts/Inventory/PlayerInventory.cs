using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace JustAGame.Inventory
{
    public class PlayerInventory : NetworkBehaviour
    {
        [SerializeField] private int defaultStartingMoney = 100;

        // Server-authoritative currency balance synced to clients
        [SyncVar(hook = nameof(OnMoneyChangedInternal))]
        private int _money;

        // Server-authoritative item list synced to clients
        private readonly SyncList<ItemData> _items = new SyncList<ItemData>();

        // Local player singleton reference and lifecycle events
        public static PlayerInventory LocalPlayer { get; private set; }
        public static event Action<PlayerInventory> OnLocalPlayerReady;
        public static event Action<PlayerInventory> OnLocalPlayerRemoved;

        public event Action<int, int> OnMoneyUpdated;
        public event Action<IReadOnlyList<ItemData>> OnInventoryUpdated;

        public int Money => _money;
        public IReadOnlyList<ItemData> Items => _items;

        public override void OnStartServer()
        {
            try
            {
                base.OnStartServer();
                _money = defaultStartingMoney;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in OnStartServer: {ex.Message}");
            }
        }

        public override void OnStartClient()
        {
            try
            {
                base.OnStartClient();
                _items.Callback += OnSyncListCallback;

                if (isOwned)
                {
                    LocalPlayer = this;
                    OnLocalPlayerReady?.Invoke(this);
                }
                else
                {
                    // Remote player instance; not local player
                }

                OnMoneyUpdated?.Invoke(_money, _money);
                OnInventoryUpdated?.Invoke(_items);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in OnStartClient: {ex.Message}");
            }
        }

        public override void OnStopClient()
        {
            try
            {
                _items.Callback -= OnSyncListCallback;

                if (isOwned && LocalPlayer == this)
                {
                    OnLocalPlayerRemoved?.Invoke(this);
                    LocalPlayer = null;
                }
                else
                {
                    // Remote player cleanup
                }

                base.OnStopClient();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in OnStopClient: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                _items.Callback -= OnSyncListCallback;

                if (LocalPlayer == this)
                {
                    LocalPlayer = null;
                }
                else
                {
                    // Not local player singleton
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in OnDestroy: {ex.Message}");
            }
        }

        private void OnMoneyChangedInternal(int oldMoney, int newMoney)
        {
            try
            {
                OnMoneyUpdated?.Invoke(newMoney, oldMoney);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in OnMoneyChangedInternal: {ex.Message}");
            }
        }

        private void OnSyncListCallback(SyncList<ItemData>.Operation op, int itemIndex, ItemData oldItem, ItemData newItem)
        {
            try
            {
                OnInventoryUpdated?.Invoke(_items);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in OnSyncListCallback: {ex.Message}");
            }
        }

        #region Server Authoritative Methods

        [Server]
        public bool HasEnoughMoney(int amount)
        {
            return amount >= 0 && _money >= amount;
        }

        [Server]
        public bool ServerDeductMoney(int amount)
        {
            try
            {
                if (amount < 0 || _money < amount)
                {
                    return false;
                }
                else
                {
                    _money -= amount;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in ServerDeductMoney: {ex.Message}");
                return false;
            }
        }

        [Server]
        public void ServerAddMoney(int amount)
        {
            try
            {
                if (amount <= 0)
                {
                    return;
                }
                else
                {
                    _money += amount;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in ServerAddMoney: {ex.Message}");
            }
        }

        [Server]
        public void ServerAddItem(ItemData item)
        {
            try
            {
                _items.Add(item);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in ServerAddItem: {ex.Message}");
            }
        }

        [Server]
        public bool ServerRemoveItem(string uniqueIdentifier)
        {
            try
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (string.Equals(_items[i].uniqueIdentifier, uniqueIdentifier, StringComparison.Ordinal))
                    {
                        _items.RemoveAt(i);
                        return true;
                    }
                    else
                    {
                        continue;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in ServerRemoveItem: {ex.Message}");
                return false;
            }
        }

        [Server]
        public void ServerSetMoney(int newAmount)
        {
            try
            {
                _money = Mathf.Max(0, newAmount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventory] Exception in ServerSetMoney: {ex.Message}");
            }
        }

        #endregion
    }
}
