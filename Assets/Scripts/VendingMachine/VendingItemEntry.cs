using System;

namespace JustAGame.VendingMachine
{
    // Item entry serialized across network from Host to all clients
    [Serializable]
    public struct VendingItemEntry : IEquatable<VendingItemEntry>
    {
        public int id;
        public string name;
        public int price;

        public VendingItemEntry(int id, string name, int price)
        {
            this.id = id;
            this.name = name ?? $"Item {id}";
            this.price = price;
        }

        public bool Equals(VendingItemEntry other)
        {
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is VendingItemEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator ==(VendingItemEntry left, VendingItemEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VendingItemEntry left, VendingItemEntry right)
        {
            return !left.Equals(right);
        }
    }
}
