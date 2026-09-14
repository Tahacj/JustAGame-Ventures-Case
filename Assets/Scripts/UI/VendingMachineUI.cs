using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using JustAGame.VendingMachine;
using JustAGame.Pooling;

namespace JustAGame.UI
{
    public class VendingMachineUI : MonoBehaviour
    {
        [SerializeField] private VendingMachine.VendingMachine targetMachine;
        [SerializeField] private VendingMachineItemButton itemButtonPrefab;
        [SerializeField] private Transform buttonsContainer;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private TextMeshProUGUI statusTMP;
        [SerializeField] private GameObject hostControlsPanel;
        [SerializeField] private Button hostCreateItemButton;

        private ObjectPool<VendingMachineItemButton> _buttonPool;
        private readonly List<VendingMachineItemButton> _activeButtons = new List<VendingMachineItemButton>();
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

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
            VendingMachine.VendingMachine.OnLocalPurchaseSuccess += HandlePurchaseSuccess;
            VendingMachine.VendingMachine.OnLocalPurchaseFailed += HandlePurchaseFailed;

            VendingMachineTrigger.OnVendingMachineEntered += HandleTriggerEntered;
            VendingMachineTrigger.OnVendingMachineExited += HandleTriggerExited;

            BindToMachine(targetMachine);
        }

        private void OnDisable()
        {
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
            CloseShop();
        }

        private void BindToMachine(VendingMachine.VendingMachine machine)
        {
            if (machine == null) return;
            targetMachine = machine;
            targetMachine.OnAvailableItemsUpdated += RefreshItemButtons;

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
            SetVisible(true);

            // Only display creation controls to the Host
            if (hostControlsPanel != null)
            {
                hostControlsPanel.SetActive(NetworkServer.active);
            }

            SetStatus("Select an item to purchase.");
        }

        public void CloseShop()
        {
            SetVisible(false);
        }

        // Toggle Canvas component so GameObject and event listeners remain active
        private void SetVisible(bool visible)
        {
            if (_canvas != null)
            {
                _canvas.enabled = visible;
            }
            else if (shopPanel != null && shopPanel != gameObject)
            {
                shopPanel.SetActive(visible);
            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(visible);
                }
            }
        }

        // Recycles buttons using ObjectPool without GC allocations
        public void RefreshItemButtons(IReadOnlyList<VendingItemEntry> items)
        {
            if (_buttonPool == null || buttonsContainer == null) return;

            ClearActiveButtons();
            if (items == null) return;

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

        // Send purchase request to server (ID only; zero-trust)
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
        }
    }
}
