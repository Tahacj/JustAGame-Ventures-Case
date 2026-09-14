using System;
using UnityEngine;
using JustAGame.Inventory;

namespace JustAGame.VendingMachine
{
    /// <summary>
    /// Trigger area component placed on the Vending Machine GameObject.
    /// Detects when the local player is in range and signals UI systems.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VendingMachineTrigger : MonoBehaviour
    {
        [Tooltip("Reference to the parent VendingMachine component.")]
        [SerializeField] private VendingMachine vendingMachine;

        public static event Action<VendingMachine> OnVendingMachineEntered;
        public static event Action<VendingMachine> OnVendingMachineExited;

        private void Awake()
        {
            if (vendingMachine == null)
            {
                vendingMachine = GetComponent<VendingMachine>() ?? GetComponentInParent<VendingMachine>();
            }

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null && inventory.isOwned)
            {
                OnVendingMachineEntered?.Invoke(vendingMachine);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null && inventory.isOwned)
            {
                OnVendingMachineExited?.Invoke(vendingMachine);
            }
        }
    }
}
