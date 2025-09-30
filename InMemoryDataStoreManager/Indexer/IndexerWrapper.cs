

using System.Reflection;

namespace InMemoryDataStoreManager.Indexer
{

    public class IndexerWrapper<T>
    {

        public void Insert(T instance)
        {
            foreach(var item in Indexers.Values)
            {
                item.Insert(instance);
            }
        }
        public void Delete(T instance)
        {
            foreach (var item in Indexers.Values)
            {
                item.Delete(instance);
            }
        }


        public IIndexer<Tkey,T>? Get<Tkey>(PropertyInfo property) => Indexers.TryGetValue(property, out var idx) ? (IIndexer<Tkey,T>)idx : null;
        public IIndexer? Get(PropertyInfo property) => Indexers.TryGetValue(property, out var idx) ? (IIndexer)idx : null;


        public void Create<TKey>(PropertyInfo property, bool is_unique) where TKey : struct, IComparable<TKey>
        {
            if (Indexers.ContainsKey(property)) return;

            var idx = new SkipList<TKey,T>(property,is_unique);

            Indexers[property] = idx;
        }

        private readonly Dictionary<PropertyInfo, IIndexer> Indexers;

        public IndexerWrapper()
        {
            Indexers = new Dictionary<PropertyInfo, IIndexer>();
        }
    }
}
