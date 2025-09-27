using System;
using System.Collections.Generic;

public class BPlusTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private readonly int _order;
    private BPlusNode _root;

    public BPlusTree()
    {
        _order = 4;
        _root = new LeafNode(4);
    }

    public BPlusTree(int order = 4)
    {
        if (order < 3)
            throw new ArgumentException("order mínimo é 3");

        _order = order;
        _root = new LeafNode(order);
    }

    public void Insert(TKey key)
    {
        Insert(key, default(TValue));
    }

    public void Insert(TKey key, TValue value)
    {
        var split = _root.Insert(key, value);
        if (split.HasValue)
        {
            // Subiu uma chave para cima -> cria novo root
            var (promotedKey, newNode) = split.Value;

            var newRoot = new InternalNode(_order);
            newRoot.Keys.Add(promotedKey);
            newRoot.Children.Add(_root);
            newRoot.Children.Add(newNode);
            _root = newRoot;
        }
    }

    public TValue? Search(TKey key) => _root.Search(key);

    public bool Delete(TKey key)
    {
        // Implementação simplificada (não faz merge completo)
        return _root.Delete(key);
    }

    // ---------------- NODES ----------------
    private abstract class BPlusNode
    {
        public List<TKey> Keys { get; } = new List<TKey>();
        public int Order { get; }

        protected BPlusNode(int order)
        {
            Order = order;
        }

        public abstract (TKey promotedKey, BPlusNode newNode)? Insert(TKey key, TValue value);
        public abstract TValue? Search(TKey key);
        public abstract bool Delete(TKey key);
    }

    private class LeafNode : BPlusNode
    {
        public List<TValue> Values { get; } = new List<TValue>();
        public LeafNode Next { get; set; }

        public LeafNode(int order) : base(order) { }

        public override (TKey promotedKey, BPlusNode newNode)? Insert(TKey key, TValue value)
        {
            // Encontra posição
            int pos = Keys.BinarySearch(key);
            if (pos >= 0)
            {
                Values[pos] = value; // Atualiza
                return null;
            }
            pos = ~pos;

            Keys.Insert(pos, key);
            Values.Insert(pos, value);

            if (Keys.Count < Order) return null;

            // Split
            int mid = Keys.Count / 2;

            var newLeaf = new LeafNode(Order);
            newLeaf.Keys.AddRange(Keys.GetRange(mid, Keys.Count - mid));
            newLeaf.Values.AddRange(Values.GetRange(mid, Values.Count - mid));

            Keys.RemoveRange(mid, Keys.Count - mid);
            Values.RemoveRange(mid, Values.Count - mid);

            newLeaf.Next = this.Next;
            this.Next = newLeaf;

            return (newLeaf.Keys[0], newLeaf);
        }

        public override TValue? Search(TKey key)
        {
            int pos = Keys.BinarySearch(key);
            return pos >= 0 ? Values[pos] : default;
        }

        public override bool Delete(TKey key)
        {
            int pos = Keys.BinarySearch(key);
            if (pos < 0) return false;
            Keys.RemoveAt(pos);
            Values.RemoveAt(pos);
            return true;
        }
    }

    private class InternalNode : BPlusNode
    {
        public List<BPlusNode> Children { get; } = new List<BPlusNode>();

        public InternalNode(int order) : base(order) { }

        public override (TKey promotedKey, BPlusNode newNode)? Insert(TKey key, TValue value)
        {
            // Encontra filho
            int idx = Keys.BinarySearch(key);
            if (idx >= 0) idx++;
            else idx = ~idx;

            var child = Children[idx];
            var split = child.Insert(key, value);

            if (split.HasValue)
            {
                var (promotedKey, newChild) = split.Value;
                Keys.Insert(idx, promotedKey);
                Children.Insert(idx + 1, newChild);

                if (Keys.Count >= Order)
                {
                    // Split internal node
                    int mid = Keys.Count / 2;
                    var newInternal = new InternalNode(Order);

                    // Move metade das chaves para novo nó
                    newInternal.Keys.AddRange(Keys.GetRange(mid + 1, Keys.Count - (mid + 1)));
                    Keys.RemoveRange(mid + 1, Keys.Count - (mid + 1));

                    // Move filhos correspondentes
                    newInternal.Children.AddRange(Children.GetRange(mid + 1, Children.Count - (mid + 1)));
                    Children.RemoveRange(mid + 1, Children.Count - (mid + 1));

                    TKey upKey = Keys[mid];
                    Keys.RemoveAt(mid);

                    return (upKey, newInternal);
                }
            }
            return null;
        }

        public override TValue? Search(TKey key)
        {
            int idx = Keys.BinarySearch(key);
            if (idx >= 0) idx++;
            else idx = ~idx;

            return Children[idx].Search(key);
        }

        public override bool Delete(TKey key)
        {
            // Simplificado: não faz rebalanceamento completo
            int idx = Keys.BinarySearch(key);
            if (idx >= 0) idx++;
            else idx = ~idx;

            return Children[idx].Delete(key);
        }
    }
}
