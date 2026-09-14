using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using JustAGame.Inventory;

namespace JustAGame.UI
{
    public class PlayerInventoryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moneyTMP;
        [SerializeField] private string moneyFormat = "Money: {0}";

        [SerializeField] private TextMeshProUGUI inventoryItemsTMP;
        [SerializeField] private string emptyInventoryPlaceholder = "Empty";
        [SerializeField] private string itemDelimiter = " - ";

        [SerializeField] private TextMeshProUGUI notificationTMP;

        // Reusable StringBuilder to eliminate GC allocations when formatting item IDs
        private readonly StringBuilder _cachedStringBuilder = new StringBuilder(128);
        private PlayerInventory _boundInventory;

        private void OnEnable()
        {
            PlayerInventory.OnLocalPlayerReady += BindInventory;
            PlayerInventory.OnLocalPlayerRemoved += UnbindInventory;

            if (PlayerInventory.LocalPlayer != null)
            {
                BindInventory(PlayerInventory.LocalPlayer);
            }
            else
            {
                UpdateMoneyDisplay(0);
                UpdateItemsDisplay(null);
            }
        }

        private void OnDisable()
        {
            PlayerInventory.OnLocalPlayerReady -= BindInventory;
            PlayerInventory.OnLocalPlayerRemoved -= UnbindInventory;

            UnbindInventory(_boundInventory);
        }

        private void OnDestroy()
        {
            UnbindInventory(_boundInventory);
        }

        private void BindInventory(PlayerInventory inventory)
        {
            if (inventory == null || _boundInventory == inventory) return;

            if (_boundInventory != null)
            {
                UnbindInventory(_boundInventory);
            }

            _boundInventory = inventory;
            _boundInventory.OnMoneyUpdated += OnMoneyChanged;
            _boundInventory.OnInventoryUpdated += OnInventoryChanged;

            UpdateMoneyDisplay(_boundInventory.Money);
            UpdateItemsDisplay(_boundInventory.Items);
        }

        private void UnbindInventory(PlayerInventory inventory)
        {
            if (_boundInventory != null && _boundInventory == inventory)
            {
                _boundInventory.OnMoneyUpdated -= OnMoneyChanged;
                _boundInventory.OnInventoryUpdated -= OnInventoryChanged;
                _boundInventory = null;
            }
        }

        private void OnMoneyChanged(int newMoney, int previousMoney)
        {
            UpdateMoneyDisplay(newMoney);
        }

        private void OnInventoryChanged(IReadOnlyList<ItemData> items)
        {
            UpdateItemsDisplay(items);
        }

        private void UpdateMoneyDisplay(int money)
        {
            if (moneyTMP != null)
            {
                moneyTMP.text = string.Format(moneyFormat, money);
            }
        }

        // Formats inventory items into "1 - 3 - 7 - 9 - 5" without GC allocations
        private void UpdateItemsDisplay(IReadOnlyList<ItemData> items)
        {
            if (items == null || items.Count == 0)
            {
                SetItemText(emptyInventoryPlaceholder);
                return;
            }

            _cachedStringBuilder.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    _cachedStringBuilder.Append(itemDelimiter);
                }
                _cachedStringBuilder.Append(items[i].id);
            }

            SetItemText(_cachedStringBuilder.ToString());
        }

        private void SetItemText(string text)
        {
            if (inventoryItemsTMP != null)
            {
                inventoryItemsTMP.text = text;
            }
        }

        public void SetNotification(string message)
        {
            if (notificationTMP != null)
            {
                notificationTMP.text = message;
            }
        }
    }
}
