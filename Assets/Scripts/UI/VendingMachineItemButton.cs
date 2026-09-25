using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JustAGame.VendingMachine;
using JustAGame.Pooling;

namespace JustAGame.UI
{
    public class VendingMachineItemButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelTMP;
        [SerializeField] private Button buyButton;

        private int _itemId;
        private Action<int> _onBuyCallback;

        private void Awake()
        {
            try
            {
                buyButton = buyButton.IsNotNull() ? buyButton : this.GetInChildrenOrNull<Button>();
                labelTMP = labelTMP.IsNotNull() ? labelTMP : this.GetInChildrenOrNull<TextMeshProUGUI>();

                if (buyButton.IsNotNull())
                {
                    buyButton.onClick.AddListener(HandleClicked);
                }
                else
                {
                    Debug.LogWarning($"[VendingMachineItemButton] Button component not found on {name}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineItemButton] Exception in Awake: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (buyButton.IsNotNull())
                {
                    buyButton.onClick.RemoveListener(HandleClicked);
                }
                else
                {
                    // No active listener to remove
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineItemButton] Exception in OnDestroy: {ex.Message}");
            }
        }

        // Configures button visuals and identifies odd/even destination
        public void Setup(VendingItemEntry entry, Action<int> onBuyCallback)
        {
            try
            {
                _itemId = entry.id;
                _onBuyCallback = onBuyCallback;

                // Odd ID -> buyer; Even ID -> host
                bool isOdd = (_itemId % 2 != 0);
                string destination = isOdd ? "<color=#80FF80>(Odd: Buyer)</color>" : "<color=#FFD700>(Even: Host)</color>";
                string displayText = $"{entry.name} (${entry.price})  {destination}";

                if (labelTMP.IsNotNull())
                {
                    labelTMP.text = displayText;
                }
                else
                {
                    Debug.LogWarning($"[VendingMachineItemButton] labelTMP is null when setting up button for item {entry.name}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineItemButton] Exception in Setup: {ex.Message}");
            }
        }

        private void HandleClicked()
        {
            try
            {
                if (_onBuyCallback.IsNotNull())
                {
                    _onBuyCallback.Invoke(_itemId);
                }
                else
                {
                    Debug.LogWarning($"[VendingMachineItemButton] No callback registered for item click (ID: {_itemId}).");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineItemButton] Exception in HandleClicked for item {_itemId}: {ex.Message}");
            }
        }
    }
}
