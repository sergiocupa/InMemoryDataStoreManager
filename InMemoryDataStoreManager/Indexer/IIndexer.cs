

using System.Collections;

namespace InMemoryDataStoreManager.Indexer
{

    public interface IIndexer<T>
    {
        void Insert(T value);
        void Delete(T value);
        bool Search(T value);
        IEnumerable<T> SearchRange(T? minValue, T? maxValue, bool includeMin = true, bool includeMax = true);
    }

    public interface IIndexer
    {
        void Insert(object value);
        void Delete(object value);
        bool Search(object value);
        IEnumerable SearchRange(object minValue, object maxValue, bool includeMin = true, bool includeMax = true);
    }
}
