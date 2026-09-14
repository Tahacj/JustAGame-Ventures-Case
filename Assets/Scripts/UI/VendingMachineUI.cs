using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using JustAGame.VendingMachine;
using JustAGame.Pooling;

namespace JustAGame.UI
{
    /// <summary>
    /// UI Controller for the Vending Machine.
    /// Uses ObjectPool to dynamically instantiate and reuse button prefabs for each available item.
    /// Synchronizes dynamically with host-created items (IDs 1, 2, 3...).
    /// Includes optional Host-Only controls for creating new items.
    /// </summary>
    public class VendingMachineUI : MonoBehaviour
    {
        [Header("Target Vending Machine")]
        [Tooltip("Optional reference to the target VendingMachine. If null, automatically finds one in the scene.")]
        [SerializeField] private VendingMachine.VendingMachine targetMachine;

        [Header("Dynamic Button Pooling")]
        [Tooltip("Prefab for individual item buttons.")]
        [SerializeField] private VendingMachineItemButton itemButtonPrefab;

        [Tooltip("Container (e.g. VerticalLayoutGroup) where item buttons are spawned.")]
        [SerializeField] private Transform buttonsContainer;

        [Header("UI Panels & Status")]
        [Tooltip("Panel containing the shop interface.")]
        [SerializeField] private GameObject shopPanel;

        [Tooltip("Status label to show purchase feedbacks or error messages.")]
        [SerializeField] private TextMeshProUGUI statusTMP;
        [SerializeField] private Text statusText;

        [Header("Host-Only Controls (Optional)")]
        [Tooltip("Panel with controls only visible to the Host (e.g. 'Create New Item' button).")]
        [SerializeField] private GameObject hostControlsPanel;
        [SerializeField] private Button hostCreateItemButton;

        // Object pool for the button prefabs
        private ObjectPool<VendingMachineItemButton> _buttonPool;
        private readonly List<VendingMachineItemButton> _activeButtons = new List<VendingMachineItemButton>();

        private void Awake()
        {
            if (itemButtonPrefab != null && buttonsContainer != null)
            {
                _buttonPool = new ObjectPool<VendingMachineItemButton>(
                    itemButtonPrefab,
                    initialCapacity: 6,
                    parent: buttonsContainer
                );
            }

            if (hostCreateItemButton != null)
            {
                hostCreateItemButton.onClick.AddListener(HandleHostCreateItemClicked);
            }
        }

        private void OnEnable()
        {
            // Subscribe to vending machine transaction feedback
            VendingMachine.VendingMachine.OnLocalPurchaseSuccess += HandlePurchaseSuccess;
            VendingMachine.VendingMachine.OnLocalPurchaseFailed += HandlePurchaseFailed;

            // Subscribe to trigger proximity events
            VendingMachineTrigger.OnVendingMachineEntered += HandleTriggerEntered;
            VendingMachineTrigger.OnVendingMachineExited += HandleTriggerExited;

            BindToMachine(targetMachine);
        }

        private void OnDisable()
        {
            // Safeguard against memory leaks
            VendingMachine.VendingMachine.OnLocalPurchaseSuccess -= HandlePurchaseSuccess;
            VendingMachine.VendingMachine.OnLocalPurchaseFailed -= HandlePurchaseFailed;

            VendingMachineTrigger.OnVendingMachineEntered -= HandleTriggerEntered;
            VendingMachineTrigger.OnVendingMachineExited -= HandleTriggerExited;

            UnbindMachine();
            ClearActiveButtons();
        }

        private void OnDestroy()
        {
            if (hostCreateItemButton != null)
            {
                hostCreateItemButton.onClick.RemoveListener(HandleHostCreateItemClicked);
            }
            ClearActiveButtons();
            _buttonPool?.Clear();
        }

        private void Start()
        {
            if (targetMachine == null)
            {
                targetMachine = FindFirstObjectByType<VendingMachine.VendingMachine>();
            }
            BindToMachine(targetMachine);
        }

        private void BindToMachine(VendingMachine.VendingMachine machine)
        {
            if (machine == null) return;
            targetMachine = machine;
            targetMachine.OnAvailableItemsUpdated += RefreshItemButtons;

            // Initial population
            RefreshItemButtons(targetMachine.AvailableItems);
        }

        private void UnbindMachine()
        {
            if (targetMachine != null)
            {
                targetMachine.OnAvailableItemsUpdated -= RefreshItemButtons;
            }
        }

        private void HandleTriggerEntered(VendingMachine.VendingMachine machine)
        {
            targetMachine = machine;
            BindToMachine(machine);
            OpenShop();
        }

        private void HandleTriggerExited(VendingMachine.VendingMachine machine)
        {
            if (targetMachine == machine)
            {
                CloseShop();
            }
        }

        public void OpenShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }

            // Only show host controls if running as Host/Server
            if (hostControlsPanel != null)
            {
                hostControlsPanel.SetActive(NetworkServer.active);
            }

            SetStatus("Select an item to purchase.");
        }

        public void CloseShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Populates item buttons using the Object Pool whenever the Host modifies the available items.
        /// Zero allocations during reuse.
        /// </summary>
        public void RefreshItemButtons(IReadOnlyList<VendingItemEntry> items)
        {
            if (_buttonPool == null || buttonsContainer == null) return;

            // Return active buttons back to the pool
            ClearActiveButtons();

            if (items == null) return;

            // Fetch and setup a pooled button for each item
            for (int i = 0; i < items.Count; i++)
            {
                VendingItemEntry entry = items[i];
                VendingMachineItemButton btn = _buttonPool.Get();
                btn.transform.SetParent(buttonsContainer, false);
                btn.Setup(entry, RequestPurchase);
                _activeButtons.Add(btn);
            }
        }

        private void ClearActiveButtons()
        {
            if (_buttonPool == null) return;

            for (int i = 0; i < _activeButtons.Count; i++)
            {
                if (_activeButtons[i] != null)
                {
                    _buttonPool.Return(_activeButtons[i]);
                }
            }
            _activeButtons.Clear();
        }

        /// <summary>
        /// Sends purchase command to the server with the selected item ID.
        /// </summary>
        public void RequestPurchase(int itemId)
        {
            if (targetMachine == null)
            {
                targetMachine = FindFirstObjectByType<VendingMachine.VendingMachine>();
            }

            if (targetMachine == null)
            {
                SetStatus("No Vending Machine found!");
                return;
            }

            SetStatus($"Purchasing Item {itemId}...");
            targetMachine.CmdRequestPurchase(itemId);
        }

        /// <summary>
        /// Host-Only action: Asks the host to create another item with an incremented ID.
        /// </summary>
        private void HandleHostCreateItemClicked()
        {
            if (targetMachine != null && NetworkServer.active)
            {
                targetMachine.CmdHostCreateItem(null, 20);
            }
        }

        private void HandlePurchaseSuccess(int itemId, string message)
        {
            SetStatus($"<color=green>{message}</color>");
        }

        private void HandlePurchaseFailed(string reason)
        {
            SetStatus($"<color=red>Error: {reason}</color>");
        }

        private void SetStatus(string message)
        {
            if (statusTMP != null)
            {
                statusTMP.text = message;
            }
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
