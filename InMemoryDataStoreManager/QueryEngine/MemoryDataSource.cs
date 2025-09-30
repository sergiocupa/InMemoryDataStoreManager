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
            Map[t] = artifact!;
        }

        public static ObjecProvider<T> Get<T>() where T : class
        {
            var t = typeof(T);
            if (!Map.TryGetValue(t, out var obj))
                throw new InvalidOperationException($"Tipo {t.Name} não registrado em MemoryContext. Chame Prepare<{t.Name}> primeiro.");
            return (ObjecProvider<T>)obj!;
        }



        private static readonly Dictionary<Type, object> Map;

        private MemoryDataSource() { }

        static MemoryDataSource()
        {
            Map = new();
        }

    }
}
