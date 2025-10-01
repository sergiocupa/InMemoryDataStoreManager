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
using System;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{

    public class MemoryDataSource
    {

        public static void Prepare<T>(Action<ObjecProvider<T>> configure) where T : class
        {
            var t = typeof(T);
            if (Map.ContainsKey(t)) return;
            var artifact = new ObjecProvider<T>();
            configure(artifact);

            var map = new ObjecProviderInfo() { ObjectProvider = artifact };
            map.Metadata.Prepare(map, artifact);

            Map[t] = map;
        }

        public static ObjecProvider<T> Get<T>() where T : class
        {
            return (ObjecProvider<T>)Get(typeof(T));
        }

        public static object Get(Type type)
        {
            if (!Map.TryGetValue(type, out var obj))
            {
                throw new InvalidOperationException($"Tipo {type.Name} não registrado em MemoryContext. Chame Prepare<{type.Name}> primeiro.");
            }
            return obj.ObjectProvider;
        }

       

        protected static readonly Dictionary<Type, ObjecProviderInfo> Map;

        protected MemoryDataSource() { }

        static MemoryDataSource()
        {
            Map = new();
        }

    }

    public class ObjecProviderInfo
    {
        internal object ObjectProvider;
        internal QueryBuildMetadata Metadata;

        public ObjecProviderInfo()
        {
            Metadata = new QueryBuildMetadata();
        }
    }


    internal class QueryBuildMetadata
    {
        private  bool       Prepared = false;
        internal MethodInfo GetIndexMethod;

        public Dictionary<PropertyInfo, IndexBuildMetadata> Indexers;

        private static BindingFlags PUBLIC_FLAG = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        internal void Prepare<T>(ObjecProviderInfo info, ObjecProvider<T> provider)
        {
            if (Prepared) Prepared = true;

            GetIndexMethod = typeof(ObjecProvider<T>).GetMethods(PUBLIC_FLAG).Where(w => w.Name == nameof(ObjecProvider<T>.GetIndex) && w.GetGenericArguments().Length == 0).FirstOrDefault();

            var index = provider.GetIndex().Indexers;
            foreach(var ix in index.Values)
            {
                Indexers.Add(ix.GetProperty(), new IndexBuildMetadata(ix, info));
            }

            Prepared = true;
        }


        internal QueryBuildMetadata()
        {
            Indexers = new Dictionary<PropertyInfo, IndexBuildMetadata>();
        }
    }

    internal class IndexBuildMetadata
    {
        internal IIndexer Index;
        internal ObjecProviderInfo Parent;
        internal MethodInfo SearchJoinGenericMethod { get; private set; }
        private Dictionary<Type, MethodInfo> SearchJoinMethods;

        private static BindingFlags PUBLIC_FLAG = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;


        internal MethodInfo MakeSearchJoinMethod(Type type)
        {
            if(!SearchJoinMethods.TryGetValue(type, out var result))
            {
                result = SearchJoinGenericMethod.MakeGenericMethod(type);
                SearchJoinMethods[type] = result;
            }
            return result!;
        }


        internal IndexBuildMetadata(IIndexer index, ObjecProviderInfo info)
        {
            Index  = index;
            Parent = info;

            SearchJoinMethods       = new Dictionary<Type, MethodInfo>();
            SearchJoinGenericMethod = index.GetType().GetMethod("SearchJoin", PUBLIC_FLAG);
        }

      



    }
}
