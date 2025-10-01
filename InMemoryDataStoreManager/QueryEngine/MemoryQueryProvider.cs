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
using InMemoryDataStoreManager.QueryEngine;
using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{

    public class MemoryQueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            return (IQueryable)Activator.CreateInstance(ArtifactGeneric, this, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MemoryQueryable<TElement>(this, expression);
        }

        public object? Execute(Expression expression) => Execute<object?>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            var parts  = Visitor.ExtractParts(expression); // devolve filtros, orderby, skip, take, join

            if (parts.ResultSelector != null && parts.InnerSource != null)
            {
                return ProviderExecutor.ExecuteJoin<TResult>(parts,  this);

                //var outerItems = ItemsField.GetValue(ObjectProvider);
               // return ProviderExecutor.ExecuteJoin<TResult>(outerItems, ElementType, parts, _LockObj);
            }

            return ProviderExecutor.ExecuteSelector<TResult>(parts,this);
        }

        internal IEnumerable<T> ApplyFiltersGeneric<T>(ObjecProvider<T> artifact, QueryParts parts) where T : class
        {
            IEnumerable<T> query = artifact.Items;

            if (parts.Filter != null)
            {
                query = ProviderApplicator.ApplyFilter(artifact, parts.Filter);
            }

            ProviderApplicator.ApplyOrder(ref query, parts);
            ProviderApplicator.ApplySkip(ref query, parts);
            ProviderApplicator.ApplyTake(ref query, parts);

            return query;
        }


        internal readonly object ObjectProvider;
        internal readonly object _LockObj;
        internal readonly Type ElementType;
        internal readonly Type ArtifactGeneric;
        internal readonly MethodInfo ToListByElement;
        internal readonly MethodInfo ApplyFiltersGeneric_Method;
        internal readonly MethodInfo JoinMethod;
        internal readonly MethodInfo SelectorMethod;
        internal readonly FieldInfo ItemsField;
        internal readonly QueryExpressionVisitor Visitor;
        internal readonly MethodInfo GetIndexMethod;
        internal readonly MethodInfo SearchJoinMethod;

        public MemoryQueryProvider(object artifactObj)
        {
            ObjectProvider = artifactObj;

            var all_flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var at = ObjectProvider.GetType();
            ElementType = at.GetGenericArguments().First();
            _LockObj = at.GetField("Lock", all_flag);
            ItemsField = at.GetField("Items");

            GetIndexMethod = at.GetMethods(all_flag).Where(w => w.Name == nameof(ObjecProvider<object>.GetIndex) && w.GetGenericArguments().Length == 0).FirstOrDefault();



           // var gt = typeof(SkipList<,>).MakeGenericType();

            //var sjm = typeof(SkipList<,>).GetMethod("SearchJoin", all_flag);



           // SearchJoinMethod = sjm.MakeGenericMethod()





            ArtifactGeneric = typeof(MemoryQueryable<>).MakeGenericType(ElementType);

            var ToListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList), BindingFlags.Static | BindingFlags.Public);
            ToListByElement = ToListMethod!.MakeGenericMethod(ElementType);


            var method = typeof(MemoryQueryProvider).GetMethod(nameof(ApplyFiltersGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!;
            ApplyFiltersGeneric_Method = method.MakeGenericMethod(ElementType);

            SelectorMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == "Select" && m.GetParameters().Length == 2);

            Visitor = new QueryExpressionVisitor();
        }

    }


}

