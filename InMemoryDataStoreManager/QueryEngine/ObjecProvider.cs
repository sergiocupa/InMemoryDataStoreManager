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

using InMemoryDataStoreManager.Indexer;
using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{

    public class ObjecProvider<T> 
    {

        public void AddIndex<TKey>(Expression<Func<T, TKey>> selector, bool is_unique) where TKey : struct, IComparable<TKey>
        {
            var body = selector.Body as MemberExpression;
            var prop = typeof(T).GetProperty(body.Member.Name);

            Indexer.Create<TKey>(prop, is_unique);
        }

        internal IIndexer<Tkey,T>? GetIndex<Tkey>(PropertyInfo property) => Indexer.Get<Tkey>(property);
        internal IIndexer? GetIndex(PropertyInfo property) => Indexer.Get(property);

        public IQueryable<T> AsQueryable() => new MemoryQueryable<T>(Provider);

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


        internal readonly DataObjectInfoLock Lock;
        private readonly  MemoryQueryProvider Provider;
        public readonly   List<T> Items;
        private readonly  IndexerWrapper<T> Indexer;

        public ObjecProvider()
        {
            Indexer  = new IndexerWrapper<T>();
            Items    = new List<T>();
            Lock     = new DataObjectInfoLock();
            Provider = new MemoryQueryProvider(this);
        }
    }

    internal class DataObjectInfoLock { }
}
