using System;
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
            try
            {
                InitializeLookup();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemCatalog] Exception in OnEnable: {ex.Message}");
            }
        }

        public void InitializeLookup()
        {
            try
            {
                _itemLookup.Clear();

                if (items.IsNull())
                {
                    Debug.LogWarning("[ItemCatalog] Items list is null during InitializeLookup.");
                    _isInitialized = false;
                    return;
                }
                else
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        ItemDefinition item = items[i];
                        if (item.IsNotNull() && !_itemLookup.ContainsKey(item.Id))
                        {
                            _itemLookup.Add(item.Id, item);
                        }
                        else
                        {
                            // Skip null items or duplicate IDs
                        }
                    }
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemCatalog] Exception in InitializeLookup: {ex.Message}");
                _isInitialized = false;
            }
        }

        // O(1) item lookup by ID
        public bool TryGetItem(int id, out ItemDefinition definition)
        {
            try
            {
                int itemsCount = items.IsNotNull() ? items.Count : 0;
                if (!_isInitialized || _itemLookup.Count != itemsCount)
                {
                    InitializeLookup();
                }
                else
                {
                    // Lookup table is up to date
                }

                return _itemLookup.TryGetValue(id, out definition);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemCatalog] Exception in TryGetItem for ID {id}: {ex.Message}");
                definition = null;
                return false;
            }
        }

        public IReadOnlyList<ItemDefinition> GetAllItems()
        {
            return items.IsNotNull() ? items : Array.Empty<ItemDefinition>();
        }
    }
}
