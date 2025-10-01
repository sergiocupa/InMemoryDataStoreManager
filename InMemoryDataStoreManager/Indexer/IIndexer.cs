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

using System.Collections;
using System.Reflection;

namespace InMemoryDataStoreManager.Indexer
{

    public interface IIndexer<Tkey, Tobj>
    {
        void Insert(Tkey key, Tobj instance);
        void Delete(Tkey value);
        bool Search(Tkey value);
        List<Tobj> Find(Tkey value);
        IEnumerable<Tobj> SearchRange(Tkey? minValue, Tkey? maxValue, bool includeMin = true, bool includeMax = true);
    }

    public interface IIndexer
    {
        void Insert(object instance);
        void Delete(object value);
        bool Search(object value);
        IEnumerable Find(object value);
        IEnumerable SearchRange(object minValue, object maxValue, bool includeMin = true, bool includeMax = true);
        PropertyInfo GetProperty();
    }

}
