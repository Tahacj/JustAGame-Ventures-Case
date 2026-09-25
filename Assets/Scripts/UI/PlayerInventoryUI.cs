using System;
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
            try
            {
                PlayerInventory.OnLocalPlayerReady += BindInventory;
                PlayerInventory.OnLocalPlayerRemoved += UnbindInventory;

                if (PlayerInventory.LocalPlayer.IsNotNull())
                {
                    BindInventory(PlayerInventory.LocalPlayer);
                }
                else
                {
                    UpdateMoneyDisplay(0);
                    UpdateItemsDisplay(null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in OnEnable: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                PlayerInventory.OnLocalPlayerReady -= BindInventory;
                PlayerInventory.OnLocalPlayerRemoved -= UnbindInventory;

                UnbindInventory(_boundInventory);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in OnDisable: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                UnbindInventory(_boundInventory);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in OnDestroy: {ex.Message}");
            }
        }

        private void BindInventory(PlayerInventory inventory)
        {
            try
            {
                if (inventory.IsNull() || _boundInventory == inventory)
                {
                    return;
                }
                else
                {
                    if (_boundInventory.IsNotNull())
                    {
                        UnbindInventory(_boundInventory);
                    }
                    else
                    {
                        // No previous inventory bound
                    }

                    _boundInventory = inventory;
                    _boundInventory.OnMoneyUpdated += OnMoneyChanged;
                    _boundInventory.OnInventoryUpdated += OnInventoryChanged;

                    UpdateMoneyDisplay(_boundInventory.Money);
                    UpdateItemsDisplay(_boundInventory.Items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in BindInventory: {ex.Message}");
            }
        }

        private void UnbindInventory(PlayerInventory inventory)
        {
            try
            {
                if (_boundInventory.IsNotNull() && _boundInventory == inventory)
                {
                    _boundInventory.OnMoneyUpdated -= OnMoneyChanged;
                    _boundInventory.OnInventoryUpdated -= OnInventoryChanged;
                    _boundInventory = null;
                }
                else
                {
                    // Target inventory was not the bound instance
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in UnbindInventory: {ex.Message}");
            }
        }

        private void OnMoneyChanged(int newMoney, int previousMoney)
        {
            try
            {
                UpdateMoneyDisplay(newMoney);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in OnMoneyChanged: {ex.Message}");
            }
        }

        private void OnInventoryChanged(IReadOnlyList<ItemData> items)
        {
            try
            {
                UpdateItemsDisplay(items);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in OnInventoryChanged: {ex.Message}");
            }
        }

        private void UpdateMoneyDisplay(int money)
        {
            try
            {
                if (moneyTMP.IsNotNull())
                {
                    moneyTMP.text = string.Format(moneyFormat, money);
                }
                else
                {
                    Debug.LogWarning("[PlayerInventoryUI] moneyTMP reference is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in UpdateMoneyDisplay: {ex.Message}");
            }
        }

        // Formats inventory items into "1 - 3 - 7 - 9 - 5" without GC allocations
        private void UpdateItemsDisplay(IReadOnlyList<ItemData> items)
        {
            try
            {
                if (items.IsNull() || items.Count == 0)
                {
                    SetItemText(emptyInventoryPlaceholder);
                    return;
                }
                else
                {
                    _cachedStringBuilder.Clear();

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (i > 0)
                        {
                            _cachedStringBuilder.Append(itemDelimiter);
                        }
                        else
                        {
                            // First element, no prefix delimiter
                        }
                        _cachedStringBuilder.Append(items[i].id);
                    }

                    SetItemText(_cachedStringBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in UpdateItemsDisplay: {ex.Message}");
            }
        }

        private void SetItemText(string text)
        {
            try
            {
                if (inventoryItemsTMP.IsNotNull())
                {
                    inventoryItemsTMP.text = text;
                }
                else
                {
                    Debug.LogWarning("[PlayerInventoryUI] inventoryItemsTMP reference is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in SetItemText: {ex.Message}");
            }
        }

        public void SetNotification(string message)
        {
            try
            {
                if (notificationTMP.IsNotNull())
                {
                    notificationTMP.text = message;
                }
                else
                {
                    Debug.LogWarning("[PlayerInventoryUI] notificationTMP reference is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryUI] Exception in SetNotification: {ex.Message}");
            }
        }
    }
}
