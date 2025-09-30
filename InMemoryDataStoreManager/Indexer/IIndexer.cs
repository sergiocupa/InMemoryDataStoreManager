

using System.Collections;

namespace InMemoryDataStoreManager.Indexer
{

    public interface IIndexer<Tkey, Tobj>
    {
        void Insert(Tkey key, Tobj instance);
        void Delete(Tkey value);
        bool Search(Tkey value);
        IEnumerable<Tobj> SearchRange(Tkey? minValue, Tkey? maxValue, bool includeMin = true, bool includeMax = true);
    }

    public interface IIndexer
    {
        void Insert(object instance);
        void Delete(object value);
        bool Search(object value);
        IEnumerable SearchRange(object minValue, object maxValue, bool includeMin = true, bool includeMax = true);
    }

}
