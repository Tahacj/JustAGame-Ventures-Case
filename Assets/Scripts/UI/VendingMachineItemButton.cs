using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JustAGame.VendingMachine;

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
            if (buyButton == null) buyButton = GetComponentInChildren<Button>();
            if (labelTMP == null) labelTMP = GetComponentInChildren<TextMeshProUGUI>();

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

        // Configures button visuals and identifies odd/even destination
        public void Setup(VendingItemEntry entry, Action<int> onBuyCallback)
        {
            _itemId = entry.id;
            _onBuyCallback = onBuyCallback;

            // Odd ID -> buyer; Even ID -> host
            bool isOdd = (_itemId % 2 != 0);
            string destination = isOdd ? "<color=#80FF80>(Odd: Buyer)</color>" : "<color=#FFD700>(Even: Host)</color>";
            string displayText = $"{entry.name} (${entry.price})  {destination}";

            if (labelTMP != null)
            {
                labelTMP.text = displayText;
            }
        }

        private void HandleClicked()
        {
            _onBuyCallback?.Invoke(_itemId);
        }
    }
}
