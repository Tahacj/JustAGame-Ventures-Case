using System;
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
            try
            {
                _canvas = this.GetComponentOrNull<Canvas>();

                if (itemButtonPrefab.IsNotNull() && buttonsContainer.IsNotNull())
                {
                    _buttonPool = new ObjectPool<VendingMachineItemButton>(
                        itemButtonPrefab,
                        initialCapacity: 6,
                        parent: buttonsContainer
                    );
                }
                else
                {
                    Debug.LogWarning("[VendingMachineUI] itemButtonPrefab or buttonsContainer is null during Awake.");
                }

                if (hostCreateItemButton.IsNotNull())
                {
                    hostCreateItemButton.onClick.AddListener(HandleHostCreateItemClicked);
                }
                else
                {
                    // No host button assigned
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in Awake: {ex.Message}");
            }
        }

        private void OnEnable()
        {
            try
            {
                VendingMachine.VendingMachine.OnLocalPurchaseSuccess += HandlePurchaseSuccess;
                VendingMachine.VendingMachine.OnLocalPurchaseFailed += HandlePurchaseFailed;

                VendingMachineTrigger.OnVendingMachineEntered += HandleTriggerEntered;
                VendingMachineTrigger.OnVendingMachineExited += HandleTriggerExited;

                BindToMachine(targetMachine);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in OnEnable: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                VendingMachine.VendingMachine.OnLocalPurchaseSuccess -= HandlePurchaseSuccess;
                VendingMachine.VendingMachine.OnLocalPurchaseFailed -= HandlePurchaseFailed;

                VendingMachineTrigger.OnVendingMachineEntered -= HandleTriggerEntered;
                VendingMachineTrigger.OnVendingMachineExited -= HandleTriggerExited;

                UnbindMachine();
                ClearActiveButtons();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in OnDisable: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (hostCreateItemButton.IsNotNull())
                {
                    hostCreateItemButton.onClick.RemoveListener(HandleHostCreateItemClicked);
                }
                else
                {
                    // No listener to remove
                }

                ClearActiveButtons();
                _buttonPool?.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in OnDestroy: {ex.Message}");
            }
        }

        private void Start()
        {
            try
            {
                if (targetMachine.IsNull())
                {
                    targetMachine = FindFirstObjectByType<VendingMachine.VendingMachine>();
                }
                else
                {
                    // Pre-assigned machine in inspector
                }

                BindToMachine(targetMachine);
                CloseShop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in Start: {ex.Message}");
            }
        }

        private void BindToMachine(VendingMachine.VendingMachine machine)
        {
            try
            {
                if (machine.IsNull())
                {
                    return;
                }
                else
                {
                    targetMachine = machine;
                    targetMachine.OnAvailableItemsUpdated += RefreshItemButtons;

                    RefreshItemButtons(targetMachine.AvailableItems);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in BindToMachine: {ex.Message}");
            }
        }

        private void UnbindMachine()
        {
            try
            {
                if (targetMachine.IsNotNull())
                {
                    targetMachine.OnAvailableItemsUpdated -= RefreshItemButtons;
                }
                else
                {
                    // Target machine already null
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in UnbindMachine: {ex.Message}");
            }
        }

        private void HandleTriggerEntered(VendingMachine.VendingMachine machine)
        {
            try
            {
                targetMachine = machine;
                BindToMachine(machine);
                OpenShop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in HandleTriggerEntered: {ex.Message}");
            }
        }

        private void HandleTriggerExited(VendingMachine.VendingMachine machine)
        {
            try
            {
                if (targetMachine == machine)
                {
                    CloseShop();
                }
                else
                {
                    // Exit from a different machine trigger
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in HandleTriggerExited: {ex.Message}");
            }
        }

        public void OpenShop()
        {
            try
            {
                SetVisible(true);

                // Only display creation controls to the Host
                if (hostControlsPanel.IsNotNull())
                {
                    hostControlsPanel.SetActive(NetworkServer.active);
                }
                else
                {
                    // Host panel not assigned
                }

                SetStatus("Select an item to purchase.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in OpenShop: {ex.Message}");
            }
        }

        public void CloseShop()
        {
            try
            {
                SetVisible(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in CloseShop: {ex.Message}");
            }
        }

        // Toggle Canvas component so GameObject and event listeners remain active
        private void SetVisible(bool visible)
        {
            try
            {
                if (_canvas.IsNotNull())
                {
                    _canvas.enabled = visible;
                }
                else if (shopPanel.IsNotNull() && shopPanel != gameObject)
                {
                    shopPanel.SetActive(visible);
                }
                else
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        if (child.IsNotNull())
                        {
                            child.gameObject.SetActive(visible);
                        }
                        else
                        {
                            // Skip destroyed child
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in SetVisible: {ex.Message}");
            }
        }

        // Recycles buttons using ObjectPool without GC allocations
        public void RefreshItemButtons(IReadOnlyList<VendingItemEntry> items)
        {
            try
            {
                if (_buttonPool.IsNull() || buttonsContainer.IsNull())
                {
                    return;
                }
                else
                {
                    ClearActiveButtons();

                    if (items.IsNull())
                    {
                        return;
                    }
                    else
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            VendingItemEntry entry = items[i];
                            VendingMachineItemButton btn = _buttonPool.Get();
                            if (btn.IsNotNull())
                            {
                                btn.transform.SetParent(buttonsContainer, false);
                                btn.Setup(entry, RequestPurchase);
                                _activeButtons.Add(btn);
                            }
                            else
                            {
                                Debug.LogWarning($"[VendingMachineUI] Button pool returned null button for item {entry.name}.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in RefreshItemButtons: {ex.Message}");
            }
        }

        private void ClearActiveButtons()
        {
            try
            {
                if (_buttonPool.IsNull())
                {
                    return;
                }
                else
                {
                    for (int i = 0; i < _activeButtons.Count; i++)
                    {
                        VendingMachineItemButton btn = _activeButtons[i];
                        if (btn.IsNotNull())
                        {
                            _buttonPool.Return(btn);
                        }
                        else
                        {
                            // Discard already destroyed instance
                        }
                    }
                    _activeButtons.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in ClearActiveButtons: {ex.Message}");
            }
        }

        // Send purchase request to server (ID only; zero-trust)
        public void RequestPurchase(int itemId)
        {
            try
            {
                if (targetMachine.IsNull())
                {
                    targetMachine = FindFirstObjectByType<VendingMachine.VendingMachine>();
                }
                else
                {
                    // Target machine already bound
                }

                if (targetMachine.IsNull())
                {
                    SetStatus("No Vending Machine found!");
                    return;
                }
                else
                {
                    SetStatus($"Purchasing Item {itemId}...");
                    targetMachine.CmdRequestPurchase(itemId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in RequestPurchase: {ex.Message}");
            }
        }

        private void HandleHostCreateItemClicked()
        {
            try
            {
                if (targetMachine.IsNotNull() && NetworkServer.active)
                {
                    targetMachine.CmdHostCreateItem(null, 20);
                }
                else
                {
                    Debug.LogWarning("[VendingMachineUI] Cannot create item: targetMachine is null or client is not server host.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in HandleHostCreateItemClicked: {ex.Message}");
            }
        }

        private void HandlePurchaseSuccess(int itemId, string message)
        {
            try
            {
                SetStatus($"<color=green>{message}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in HandlePurchaseSuccess: {ex.Message}");
            }
        }

        private void HandlePurchaseFailed(string reason)
        {
            try
            {
                SetStatus($"<color=red>Error: {reason}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in HandlePurchaseFailed: {ex.Message}");
            }
        }

        private void SetStatus(string message)
        {
            try
            {
                if (statusTMP.IsNotNull())
                {
                    statusTMP.text = message;
                }
                else
                {
                    Debug.LogWarning("[VendingMachineUI] statusTMP is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineUI] Exception in SetStatus: {ex.Message}");
            }
        }
    }
}
