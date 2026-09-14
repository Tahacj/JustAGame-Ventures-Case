using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JustAGame.Inventory;

namespace JustAGame.UI
{
    /// <summary>
    /// UI Presenter for the Player's Inventory and Currency.
    /// Listens to events from the local player's PlayerInventory component.
    /// Memory leak safe: completely unbinds on disable/destroy.
    /// Allocation efficient: uses a reusable StringBuilder to format "1 - 3 - 7 - 9 - 5".
    /// </summary>
    public class PlayerInventoryUI : MonoBehaviour
    {
        [Header("Money Display")]
        [Tooltip("TextMeshPro label to display player money.")]
        [SerializeField] private TextMeshProUGUI moneyTMP;
        [Tooltip("Fallback standard UI Text label for money (if TextMeshPro is not used).")]
        [SerializeField] private Text moneyText;
        [SerializeField] private string moneyFormat = "Money: {0}";

        [Header("Inventory Items Display")]
        [Tooltip("TextMeshPro label to display items formatted as: 1 - 3 - 7 - 9 - 5")]
        [SerializeField] private TextMeshProUGUI inventoryItemsTMP;
        [Tooltip("Fallback standard UI Text label for items.")]
        [SerializeField] private Text inventoryItemsText;
        [SerializeField] private string emptyInventoryPlaceholder = "Empty";
        [SerializeField] private string itemDelimiter = " - ";

        [Header("Optional Notification Display")]
        [Tooltip("Optional label to show purchase feedbacks or messages.")]
        [SerializeField] private TextMeshProUGUI notificationTMP;

        // Cached StringBuilder to prevent GC allocations on item list formatting
        private readonly StringBuilder _cachedStringBuilder = new StringBuilder(128);

        // Tracked local inventory instance
        private PlayerInventory _boundInventory;

        private void OnEnable()
        {
            // Subscribe to local player lifecycle events
            PlayerInventory.OnLocalPlayerReady += BindInventory;
            PlayerInventory.OnLocalPlayerRemoved += UnbindInventory;

            // If local player already exists, bind immediately
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
            // Safeguard: Unsubscribe from static events to prevent memory leaks
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

            // Unbind previous if any
            if (_boundInventory != null)
            {
                UnbindInventory(_boundInventory);
            }

            _boundInventory = inventory;

            // Subscribe to inventory update events
            _boundInventory.OnMoneyUpdated += OnMoneyChanged;
            _boundInventory.OnInventoryUpdated += OnInventoryChanged;

            // Initial view update
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
            string formatted = string.Format(moneyFormat, money);

            if (moneyTMP != null)
            {
                moneyTMP.text = formatted;
            }
            if (moneyText != null)
            {
                moneyText.text = formatted;
            }
        }

        /// <summary>
        /// Formats item IDs into the requested format: "1 - 3 - 7 - 9 - 5"
        /// Uses StringBuilder without allocating intermediate string objects.
        /// </summary>
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
            if (inventoryItemsText != null)
            {
                inventoryItemsText.text = text;
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
