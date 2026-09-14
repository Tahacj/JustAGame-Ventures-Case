using System.Collections.Generic;
using UnityEngine;

namespace JustAGame.Inventory
{
    /// <summary>
    /// Authoritative database of all purchasable items.
    /// Used by the server/host to validate item IDs and enforce accurate prices.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "JustAGame/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private readonly Dictionary<int, ItemDefinition> _itemLookup = new Dictionary<int, ItemDefinition>();
        private bool _isInitialized;

        private void OnEnable()
        {
            InitializeLookup();
        }

        public void InitializeLookup()
        {
            _itemLookup.Clear();
            if (items == null) return;

            foreach (var item in items)
            {
                if (item != null && !_itemLookup.ContainsKey(item.Id))
                {
                    _itemLookup.Add(item.Id, item);
                }
            }
            _isInitialized = true;
        }

        /// <summary>
        /// Attempt to retrieve item details by ID in O(1) time.
        /// </summary>
        public bool TryGetItem(int id, out ItemDefinition definition)
        {
            if (!_isInitialized || _itemLookup.Count != items.Count)
            {
                InitializeLookup();
            }

            return _itemLookup.TryGetValue(id, out definition);
        }

        public IReadOnlyList<ItemDefinition> GetAllItems()
        {
            return items;
        }
    }
}
