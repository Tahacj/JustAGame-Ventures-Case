using System;
using UnityEngine;
using JustAGame.Inventory;

namespace JustAGame.VendingMachine
{
    [RequireComponent(typeof(Collider))]
    public class VendingMachineTrigger : MonoBehaviour
    {
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

            // Kinematic Rigidbody ensures CharacterController triggers PhysX collision events
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only trigger UI for the local player
            var inventory = other.GetComponent<PlayerInventory>() ?? other.GetComponentInParent<PlayerInventory>();
            if (inventory != null && inventory.isOwned)
            {
                OnVendingMachineEntered?.Invoke(vendingMachine);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var inventory = other.GetComponent<PlayerInventory>() ?? other.GetComponentInParent<PlayerInventory>();
            if (inventory != null && inventory.isOwned)
            {
                OnVendingMachineExited?.Invoke(vendingMachine);
            }
        }
    }
}
