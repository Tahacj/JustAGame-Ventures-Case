using System.Collections.Generic;
using UnityEngine;

namespace JustAGame.Inventory
{
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

        // O(1) item lookup by ID
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
