using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace JustAGame.Inventory
{
    /// <summary>
    /// Server-authoritative inventory tracking a player's items and currency balance.
    /// Synchronizes state to clients using Mirror SyncVar and SyncList.
    /// Guarantees that clients cannot tamper with inventory state or money.
    /// </summary>
    public class PlayerInventory : NetworkBehaviour
    {
        [Header("Starting Balance")]
        [Tooltip("Initial currency granted to the player upon spawning on the server.")]
        [SerializeField] private int defaultStartingMoney = 100;

        /// <summary>
        /// Authoritative money balance. Hook invoked whenever the server mutates value.
        /// </summary>
        [SyncVar(hook = nameof(OnMoneyChangedInternal))]
        private int _money;

        /// <summary>
        /// Authoritative synchronized list of owned items.
        /// Client-side mutations are disallowed by Mirror.
        /// </summary>
        private readonly SyncList<ItemData> _items = new SyncList<ItemData>();

        /// <summary>
        /// Convenience static reference to the local human player's inventory instance.
        /// </summary>
        public static PlayerInventory LocalPlayer { get; private set; }

        /// <summary>
        /// Static event fired when the local player's inventory is initialized.
        /// UI presenters can subscribe to this without coupling to scene loading order.
        /// </summary>
        public static event Action<PlayerInventory> OnLocalPlayerReady;
        public static event Action<PlayerInventory> OnLocalPlayerRemoved;

        /// <summary>
        /// Instance event invoked whenever the money balance changes. (newAmount, previousAmount)
        /// </summary>
        public event Action<int, int> OnMoneyUpdated;

        /// <summary>
        /// Instance event invoked whenever items are added, removed, or refreshed.
        /// Passes the read-only view of items to prevent consumer modifications.
        /// </summary>
        public event Action<IReadOnlyList<ItemData>> OnInventoryUpdated;

        public int Money => _money;
        public IReadOnlyList<ItemData> Items => _items;

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Server initializes the starting money
            _money = defaultStartingMoney;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Subscribe to SyncList changes
            _items.Callback += OnSyncListCallback;

            if (isOwned)
            {
                LocalPlayer = this;
                OnLocalPlayerReady?.Invoke(this);
            }

            // Trigger initial UI state
            OnMoneyUpdated?.Invoke(_money, _money);
            OnInventoryUpdated?.Invoke(_items);
        }

        public override void OnStopClient()
        {
            // Unsubscribe to avoid memory leaks
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
            // Safeguard cleanup for memory leaks
            _items.Callback -= OnSyncListCallback;
            if (LocalPlayer == this)
            {
                LocalPlayer = null;
            }
        }

        #region State Synchronization Hooks

        private void OnMoneyChangedInternal(int oldMoney, int newMoney)
        {
            OnMoneyUpdated?.Invoke(newMoney, oldMoney);
        }

        private void OnSyncListCallback(SyncList<ItemData>.Operation op, int itemIndex, ItemData oldItem, ItemData newItem)
        {
            // Forward readonly list to listeners so UI can re-render cleanly
            OnInventoryUpdated?.Invoke(_items);
        }

        #endregion

        #region Server Authoritative Methods

        /// <summary>
        /// Checks if the player possesses sufficient funds.
        /// </summary>
        [Server]
        public bool HasEnoughMoney(int amount)
        {
            return amount >= 0 && _money >= amount;
        }

        /// <summary>
        /// Deducts money strictly on the server.
        /// Returns true if deduction was successful, false if insufficient funds.
        /// </summary>
        [Server]
        public bool ServerDeductMoney(int amount)
        {
            if (amount < 0 || _money < amount)
            {
                return false;
            }

            _money -= amount;
            return true;
        }

        /// <summary>
        /// Adds money strictly on the server.
        /// </summary>
        [Server]
        public void ServerAddMoney(int amount)
        {
            if (amount <= 0) return;
            _money += amount;
        }

        /// <summary>
        /// Adds an item strictly on the server.
        /// </summary>
        [Server]
        public void ServerAddItem(ItemData item)
        {
            _items.Add(item);
        }

        /// <summary>
        /// Removes an item by its unique identifier on the server.
        /// </summary>
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

        /// <summary>
        /// Resets the starting balance (server only).
        /// </summary>
        [Server]
        public void ServerSetMoney(int newAmount)
        {
            _money = Mathf.Max(0, newAmount);
        }

        #endregion
    }
}
