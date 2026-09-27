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
            try
            {
                if (vendingMachine.IsNull())
                {
                    vendingMachine = this.GetComponentOrNull<VendingMachine>();
                    if (vendingMachine.IsNull())
                    {
                        vendingMachine = this.GetComponentInParentOrNull<VendingMachine>();
                    }
                    else
                    {
                        // Found on current GameObject
                    }
                }
                else
                {
                    // Pre-assigned in inspector
                }

                var col = this.GetComponentOrNull<Collider>();
                if (col.IsNotNull())
                {
                    col.isTrigger = true;
                }
                else
                {
                    Debug.LogWarning($"[VendingMachineTrigger] Collider not found on {name}.");
                }

                // Kinematic Rigidbody ensures CharacterController triggers PhysX collision events
                var rb = this.GetComponentOrNull<Rigidbody>();
                if (rb.IsNull())
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                else
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineTrigger] Exception in Awake: {ex.Message}");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            try
            {
                if (other.IsNull())
                {
                    return;
                }
                else
                {
                    // Only trigger UI for the local player
                    var inventory = other.GetComponentOrNull<PlayerInventory>();
                    if (inventory.IsNull())
                    {
                        inventory = other.GetComponentInParentOrNull<PlayerInventory>();
                    }
                    else
                    {
                        // Inventory component resolved directly on collider
                    }

                    if (inventory.IsNotNull() && inventory.isOwned)
                    {
                        OnVendingMachineEntered?.Invoke(vendingMachine);
                    }
                    else
                    {
                        // Ignore collision from non-player or non-owned entities
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineTrigger] Exception in OnTriggerEnter: {ex.Message}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            try
            {
                if (other.IsNull())
                {
                    return;
                }
                else
                {
                    var inventory = other.GetComponentOrNull<PlayerInventory>();
                    if (inventory.IsNull())
                    {
                        inventory = other.GetComponentInParentOrNull<PlayerInventory>();
                    }
                    else
                    {
                        // Inventory component resolved directly on collider
                    }

                    if (inventory.IsNotNull() && inventory.isOwned)
                    {
                        OnVendingMachineExited?.Invoke(vendingMachine);
                    }
                    else
                    {
                        // Ignore trigger exit from non-player entities
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VendingMachineTrigger] Exception in OnTriggerExit: {ex.Message}");
            }
        }
    }
}
