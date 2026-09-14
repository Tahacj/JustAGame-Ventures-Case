using System;
using UnityEngine;

namespace JustAGame.Inventory
{
    // Item configuration stored in catalog
    [Serializable]
    public class ItemDefinition
    {
        [SerializeField] private int id;
        [SerializeField] private string itemName = "New Item";
        [SerializeField] private int price = 10;
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
