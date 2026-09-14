using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using JustAGame.Inventory;
using JustAGame.Core.Network;

namespace JustAGame.VendingMachine
{
    public class VendingMachine : NetworkBehaviour
    {
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private int initialHostItemCount = 4;
        [SerializeField] private int baseItemPrice = 15;
        [SerializeField] private float maxInteractionDistance = 5.0f;
        [SerializeField] private float purchaseCooldownSeconds = 0.5f;

        [SyncVar] private int _nextAutoIncrementId = 1;
        private readonly SyncList<VendingItemEntry> _availableItems = new SyncList<VendingItemEntry>();
        private readonly Dictionary<int, float> _cooldownTracker = new Dictionary<int, float>();

        public static event Action<int, string> OnLocalPurchaseSuccess;
        public static event Action<string> OnLocalPurchaseFailed;
        public event Action<IReadOnlyList<VendingItemEntry>> OnAvailableItemsUpdated;

        public ItemCatalog Catalog => catalog;
        public IReadOnlyList<VendingItemEntry> AvailableItems => _availableItems;

        public override void OnStartServer()
        {
            base.OnStartServer();

            // Populate initial items only if list is empty
            if (_availableItems.Count == 0)
            {
                _nextAutoIncrementId = 1;
                for (int i = 0; i < initialHostItemCount; i++)
                {
                    ServerCreateNewItem($"Item #{_nextAutoIncrementId}", baseItemPrice + (i * 5));
                }
            }
        }

        public override void OnStopServer()
        {
            _availableItems.Clear();
            _nextAutoIncrementId = 1;
            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _availableItems.Callback += HandleAvailableItemsChanged;
            OnAvailableItemsUpdated?.Invoke(_availableItems);
        }

        public override void OnStopClient()
        {
            _availableItems.Callback -= HandleAvailableItemsChanged;
            base.OnStopClient();
        }

        private void OnDestroy()
        {
            _availableItems.Callback -= HandleAvailableItemsChanged;
        }

        private void HandleAvailableItemsChanged(SyncList<VendingItemEntry>.Operation op, int itemIndex, VendingItemEntry oldItem, VendingItemEntry newItem)
        {
            OnAvailableItemsUpdated?.Invoke(_availableItems);
        }

        #region Host Item Creation

        // Server-authoritative item creation with sequential IDs
        [Server]
        public void ServerCreateNewItem(string itemName = null, int price = 15)
        {
            int assignedId = _nextAutoIncrementId++;
            string finalName = string.IsNullOrEmpty(itemName) ? $"Item #{assignedId}" : itemName;
            var entry = new VendingItemEntry(assignedId, finalName, price);

            _availableItems.Add(entry);
        }

        // Host client command to create new item (rejects non-host senders)
        [Command(requiresAuthority = false)]
        public void CmdHostCreateItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            if (sender != NetworkServer.localConnection)
            {
                TargetRpcPurchaseFailed(sender, "Only the Host is authorized to create items.");
                return;
            }

            ServerCreateNewItem(itemName, price);
        }

        #endregion

        #region Purchase Validation Pipeline

        // Server-authoritative purchase validation (Zero-Trust)
        [Command(requiresAuthority = false)]
        public void CmdRequestPurchase(int itemId, NetworkConnectionToClient sender = null)
        {
            if (sender == null || sender.identity == null) return;

            int connectionId = sender.connectionId;
            float currentTime = Time.time;

            // 1. Anti-spam cooldown
            if (_cooldownTracker.TryGetValue(connectionId, out float lastTime))
            {
                if (currentTime - lastTime < purchaseCooldownSeconds)
                {
                    TargetRpcPurchaseFailed(sender, "Please wait a moment before purchasing again.");
                    return;
                }
            }
            _cooldownTracker[connectionId] = currentTime;

            // 2. Proximity distance check
            GameObject playerObj = sender.identity.gameObject;
            float distance = Vector3.Distance(playerObj.transform.position, transform.position);
            if (distance > maxInteractionDistance)
            {
                TargetRpcPurchaseFailed(sender, "You are too far from the vending machine.");
                return;
            }

            // 3. Resolve item price from dynamic list or static catalog
            int itemPrice = -1;
            string itemName = $"Item {itemId}";

            for (int i = 0; i < _availableItems.Count; i++)
            {
                if (_availableItems[i].id == itemId)
                {
                    itemPrice = _availableItems[i].price;
                    itemName = _availableItems[i].name;
                    break;
                }
            }

            if (itemPrice < 0 && catalog != null && catalog.TryGetItem(itemId, out ItemDefinition itemDef))
            {
                itemPrice = itemDef.Price;
                itemName = itemDef.ItemName;
            }

            if (itemPrice < 0)
            {
                TargetRpcPurchaseFailed(sender, $"Item ID {itemId} is not available in the vending machine.");
                return;
            }

            // 4. Buyer balance check
            var buyerInventory = playerObj.GetComponent<PlayerInventory>();
            if (buyerInventory == null || !buyerInventory.HasEnoughMoney(itemPrice))
            {
                int balance = buyerInventory != null ? buyerInventory.Money : 0;
                TargetRpcPurchaseFailed(sender, $"Insufficient funds. Item costs ${itemPrice}, but you have ${balance}.");
                return;
            }

            // 5. Server currency deduction
            if (!buyerInventory.ServerDeductMoney(itemPrice))
            {
                TargetRpcPurchaseFailed(sender, "Transaction failed while deducting funds.");
                return;
            }

            // 6. Generate server GUID
            string uniqueId = Guid.NewGuid().ToString("N");
            ItemData purchasedItem = new ItemData(itemId, uniqueId);

            // 7. Odd/Even allocation rule: Odd -> Buyer, Even -> Host
            bool isOdd = (itemId % 2 != 0);

            if (isOdd)
            {
                buyerInventory.ServerAddItem(purchasedItem);
                TargetRpcPurchaseSuccess(sender, itemId, $"Purchased Item {itemId} ({itemName})! Added to YOUR inventory (Odd ID).");
            }
            else
            {
                PlayerInventory hostInventory = GameNetworkManager.GetHostInventory();
                if (hostInventory != null)
                {
                    hostInventory.ServerAddItem(purchasedItem);
                    TargetRpcPurchaseSuccess(sender, itemId, $"Purchased Item {itemId} ({itemName})! Added to HOST's inventory (Even ID).");
                }
                else
                {
                    // Fallback to buyer if host player object is unavailable
                    buyerInventory.ServerAddItem(purchasedItem);
                    TargetRpcPurchaseSuccess(sender, itemId, $"Purchased Item {itemId}! Host not found, redirected to your inventory.");
                }
            }
        }

        #endregion

        #region Target RPC Responses

        [TargetRpc]
        private void TargetRpcPurchaseSuccess(NetworkConnection target, int itemId, string message)
        {
            OnLocalPurchaseSuccess?.Invoke(itemId, message);
        }

        [TargetRpc]
        private void TargetRpcPurchaseFailed(NetworkConnection target, string reason)
        {
            OnLocalPurchaseFailed?.Invoke(reason);
        }

        #endregion
    }
}
