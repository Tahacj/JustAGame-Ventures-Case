using System;
using UnityEngine;

namespace JustAGame.Inventory
{
    /// <summary>
    /// Configuration data for an item type in the game catalog.
    /// Kept authoritative on the server/host to prevent client-side price tampering.
    /// </summary>
    [Serializable]
    public class ItemDefinition
    {
        [Tooltip("Unique integer ID of the item.")]
        [SerializeField] private int id;

        [Tooltip("Display name of the item.")]
        [SerializeField] private string itemName = "New Item";

        [Tooltip("Authoritative base purchase price.")]
        [SerializeField] private int price = 10;

        [Tooltip("Optional visual icon for the item.")]
        [SerializeField] private Sprite icon;

        public int Id => id;
        public string ItemName => itemName;
        public int Price => price;
        public Sprite Icon => icon;

        public ItemDefinition() { }

        public ItemDefinition(int id, string itemName, int price, Sprite icon = null)
        {
            this.id = id;
            this.itemName = itemName;
            this.price = price;
            this.icon = icon;
        }
    }
}
