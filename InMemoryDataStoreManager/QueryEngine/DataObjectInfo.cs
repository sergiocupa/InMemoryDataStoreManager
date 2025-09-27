using InMemoryDataStoreManager.Indexer;
using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{

    public class DataObjectInfo<T> 
    {

        public void AddIndex<TKey>(Expression<Func<T, TKey>> selector) where TKey : struct, IComparable<TKey>
        {
            var body = selector.Body as MemberExpression;
            var prop = typeof(T).GetProperty(body.Member.Name);

            Indexer.Create<TKey>(prop);
        }

        internal IIndexer<Tkey>? GetIndex<Tkey>(PropertyInfo property) => Indexer.Get<Tkey>(property);

        public IQueryable<T> AsQueryable() => new MemoryQueryable<T>(this);

        public void Save(T item)
        {
            Items.Add(item);
            Indexer.Insert(item);  
        }

        public void Delete(T item)
        {
            Items.Remove(item);
            Indexer.Delete(item);
        }


        public readonly List<T> Items;
        private readonly IndexerWrapper<T> Indexer;

        public DataObjectInfo()
        {
            Indexer = new IndexerWrapper<T>();
            Items   = new List<T>();
        }
    }
}
