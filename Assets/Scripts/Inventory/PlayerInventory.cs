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
            base.OnStartServer();
            _money = defaultStartingMoney;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _items.Callback += OnSyncListCallback;

            if (isOwned)
            {
                LocalPlayer = this;
                OnLocalPlayerReady?.Invoke(this);
            }

            OnMoneyUpdated?.Invoke(_money, _money);
            OnInventoryUpdated?.Invoke(_items);
        }

        public override void OnStopClient()
        {
            _items.Callback -= OnSyncListCallback;

            if (isOwned && LocalPlayer == this)
            {
                OnLocalPlayerRemoved?.Invoke(this);
                LocalPlayer = null;
            }

            base.OnStopClient();
        }

        private void OnDestroy()
        {
            _items.Callback -= OnSyncListCallback;
            if (LocalPlayer == this)
            {
                LocalPlayer = null;
            }
        }

        private void OnMoneyChangedInternal(int oldMoney, int newMoney)
        {
            OnMoneyUpdated?.Invoke(newMoney, oldMoney);
        }

        private void OnSyncListCallback(SyncList<ItemData>.Operation op, int itemIndex, ItemData oldItem, ItemData newItem)
        {
            OnInventoryUpdated?.Invoke(_items);
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
            if (amount < 0 || _money < amount) return false;
            _money -= amount;
            return true;
        }

        [Server]
        public void ServerAddMoney(int amount)
        {
            if (amount <= 0) return;
            _money += amount;
        }

        [Server]
        public void ServerAddItem(ItemData item)
        {
            _items.Add(item);
        }

        [Server]
        public bool ServerRemoveItem(string uniqueIdentifier)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].uniqueIdentifier, uniqueIdentifier, StringComparison.Ordinal))
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        [Server]
        public void ServerSetMoney(int newAmount)
        {
            _money = Mathf.Max(0, newAmount);
        }

        #endregion
    }
}
