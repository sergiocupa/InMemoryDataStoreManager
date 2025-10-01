//  MIT License – Modified for Mandatory Attribution
//  
//  Copyright(c) 2025 Sergio Paludo
//
//  github.com/sergiocupa
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files, 
//  to use, copy, modify, merge, publish, distribute, and sublicense the software, including for commercial purposes, provided that:
//  
//     01. The original author’s credit is retained in all copies of the source code;
//     02. The original author’s credit is included in any code generated, derived, or distributed from this software, including templates, libraries, or code - generating scripts.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.


using InMemoryDataStoreManager.Engine;
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
        public IIndexer? Get(PropertyInfo property)
        {
            Indexers.TryGetValue(property, out var idx);// ? (IIndexer)idx : null;
            return idx;
        }


        public void Create<TKey>(PropertyInfo property, bool is_unique) where TKey : struct, IComparable<TKey>
        {
            if (Indexers.ContainsKey(property)) return;

            var idx = new SkipList<TKey,T>(property,is_unique);

            Indexers[property] = idx;
        }

        internal readonly Dictionary<PropertyInfo, IIndexer> Indexers;

        public IndexerWrapper()
        {
            Indexers = new Dictionary<PropertyInfo, IIndexer>();
        }
    }


    internal class MemoryDataSourceWrapper : MemoryDataSource
    {
        internal static IndexBuildMetadata Get(PropertyInfo prop)
        {
            IndexBuildMetadata result = null;
            if (Map.TryGetValue(prop.DeclaringType, out var rm))
            {
                rm.Metadata.Indexers.TryGetValue(prop, out result);
            }
            return result;
        }

        private MemoryDataSourceWrapper() : base() { }

    }
}
