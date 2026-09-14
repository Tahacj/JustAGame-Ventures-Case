using System;

namespace JustAGame.Inventory
{
    // Network-serialized item instance tracking unique server GUID
    [Serializable]
    public struct ItemData : IEquatable<ItemData>
    {
        public int id;
        public string uniqueIdentifier;

        public ItemData(int id, string uniqueIdentifier)
        {
            this.id = id;
            this.uniqueIdentifier = uniqueIdentifier ?? string.Empty;
        }

        public bool Equals(ItemData other)
        {
            return id == other.id && string.Equals(uniqueIdentifier, other.uniqueIdentifier, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ItemData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (id * 397) ^ (uniqueIdentifier != null ? StringComparer.Ordinal.GetHashCode(uniqueIdentifier) : 0);
            }
        }

        public static bool operator ==(ItemData left, ItemData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemData left, ItemData right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Item(ID: {id}, UID: {uniqueIdentifier})";
        }
    }
}
