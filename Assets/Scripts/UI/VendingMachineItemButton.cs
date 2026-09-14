using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JustAGame.VendingMachine;

namespace JustAGame.UI
{
    /// <summary>
    /// UI Button element for an item in the Vending Machine.
    /// Designed as a pooled prefab instantiated by VendingMachineUI.
    /// </summary>
    public class VendingMachineItemButton : MonoBehaviour
    {
        [Header("UI Bindings")]
        [Tooltip("Text label displaying item info (ID, Name, Price, Odd/Even).")]
        [SerializeField] private TextMeshProUGUI labelTMP;
        [Tooltip("Fallback standard UI Text label.")]
        [SerializeField] private Text labelText;
        [Tooltip("Button component that triggers the purchase.")]
        [SerializeField] private Button buyButton;

        private int _itemId;
        private Action<int> _onBuyCallback;

        private void Awake()
        {
            if (buyButton == null)
            {
                buyButton = GetComponentInChildren<Button>();
            }
            if (labelTMP == null)
            {
                labelTMP = GetComponentInChildren<TextMeshProUGUI>();
            }
            if (labelText == null)
            {
                labelText = GetComponentInChildren<Text>();
            }

            if (buyButton != null)
            {
                buyButton.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Setup(VendingItemEntry entry, Action<int> onBuyCallback)
        {
            _itemId = entry.id;
            _onBuyCallback = onBuyCallback;

            bool isOdd = (_itemId % 2 != 0);
            string destination = isOdd ? "<color=#80FF80>(Odd: Buyer)</color>" : "<color=#FFD700>(Even: Host)</color>";
            string displayText = $"Item {_itemId} - {entry.name} (${entry.price}) {destination}";

            if (labelTMP != null)
            {
                labelTMP.text = displayText;
            }
            if (labelText != null)
            {
                labelText.text = displayText;
            }
        }

        private void HandleClicked()
        {
            _onBuyCallback?.Invoke(_itemId);
        }
    }
}
