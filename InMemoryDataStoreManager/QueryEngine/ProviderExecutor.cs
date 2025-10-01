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
using InMemoryDataStoreManager.Indexer;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.QueryEngine
{

    internal class ProviderExecutor
    {

        public static TResult ExecuteSelector<TResult>(QueryParts parts, MemoryQueryProvider provider)
        {
            var result = provider.ApplyFiltersGeneric_Method.Invoke(provider, new object[] { provider.ObjectProvider, parts })!;

            var list = default(object);

            if (parts.ResultSelector != null)
            {
                var rtype = parts.ResultSelector.Body.Type;

                if (rtype == provider.ElementType)
                {
                    lock (provider._LockObj)
                    {
                        list = provider.ToListByElement.Invoke(null, new object[] { result });
                    }
                }
                else
                {
                    list = ExecuteSelector(parts.ResultSelector, provider._LockObj, (selector) =>
                    {
                        var selectMethod = provider.SelectorMethod.MakeGenericMethod(result.GetType().GetGenericArguments()[0], parts.ResultSelector.Body.Type);
                        var projected = selectMethod.Invoke(null, new object[] { result, selector });
                        return projected;
                    });
                    return (TResult)list;
                }
            }
            else
            {
                lock (provider._LockObj)
                {
                    list = provider.ToListByElement.Invoke(null, new object[] { result });
                }
            }
            return (TResult)list;
        }

        internal static object ExecuteSelector(LambdaExpression selector, object lock_obj, Func<Delegate, object> execute_before)
        {
            var list_method = TO_LIST_METHOD.MakeGenericMethod(selector.Body.Type);

            var res = execute_before(selector.Compile());

            var jist = default(object);
            lock (lock_obj)
            {
                jist = list_method.Invoke(null, new object[] { res });
            }

            return jist;
        }

        internal static TResult ExecuteJoin<TResult>(QueryParts parts, MemoryQueryProvider provider)
        {
            var outerItems = provider.ItemsField.GetValue(provider.ObjectProvider);

            var outerProp = ExtractProperty(parts.OuterKeySelector);
            var innerProp = ExtractProperty(parts.InnerKeySelector);
            //var left_ix = (IIndexer)provider.GetIndexMethod.Invoke(provider.ObjectProvider, new object[] { outerProp });
            var left_ix   = MemoryDataSourceWrapper.Get(outerProp);
            var right_ix  = MemoryDataSourceWrapper.Get(innerProp);

            if(left_ix != null && right_ix != null)
            {
                var met = left_ix.MakeSearchJoinMethod(innerProp.DeclaringType);

                var joinPairs = (IEnumerable)met.Invoke(left_ix.Index, new object[] { right_ix.Index });
                var selector  = parts.ResultSelector.Compile();
                var result    = JOIN_SELECTOR_METHOD.Invoke( null, new object[] { joinPairs, selector } );
                return (TResult)(object)result;
            }
            else
            {
                return ExecuteJoin<TResult>(outerItems, provider.ElementType, parts, provider._LockObj);
            }
        }

        internal static TResult ExecuteJoin<TResult>(object outer_itens, Type outer_type, QueryParts parts, object lock_obj)
        {
            var touter_key = parts.OuterKeySelector.Body.Type;
            var innerType  = parts.InnerSource.ElementType;
            var rtype      = parts.ResultSelector.Body.Type;
            var joinMethod = JOIN_METHOD.MakeGenericMethod(outer_type, innerType, touter_key, rtype);

            var compiledOuterKey = parts.OuterKeySelector.Compile();
            var compiledInnerKey = parts.InnerKeySelector.Compile();

            var jist = ExecuteSelector(parts.ResultSelector, lock_obj, (selector) =>
            {
                var joined = joinMethod.Invoke(null, new object[] { outer_itens, parts.InnerSource, compiledOuterKey, compiledInnerKey, selector });
                return joined;
            });

            return (TResult)jist;
        }


        public static List<TResult> ApplyJoinSelector<TLeft, TRight, TResult>(IEnumerable<(TLeft Left, TRight Right)> pairs, Func<TLeft, TRight, TResult> selector)
        {
            var result = new List<TResult>();
            foreach (var (l, r) in pairs)
            {
                result.Add(selector(l, r));
            }
            return result;
        }

        private static PropertyInfo ExtractProperty(LambdaExpression lambda)
        {
            if (lambda.Body is MemberExpression member && member.Member is PropertyInfo pi)
                return pi;

            throw new InvalidOperationException("Key selector não é uma propriedade");
        }

        private static PropertyInfo? GetPropertyFromLambda(LambdaExpression lambda)
        {
            Expression body = lambda.Body;

            // Se tiver conversão explícita (Convert), ignora
            if (body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert) body = ue.Operand;

            if (body is MemberExpression me && me.Member is PropertyInfo pi) return pi;

            // Não suportado (ex: objetos anônimos, múltiplos campos)
            return null;
        }


        static MethodInfo JOIN_METHOD;
        static MethodInfo TO_LIST_METHOD;
        static MethodInfo JOIN_SELECTOR_METHOD;

        static ProviderExecutor()
        {
            JOIN_METHOD          = typeof(Enumerable).GetMethods().First(m => m.Name == "Join" && m.GetParameters().Length == 5);
            TO_LIST_METHOD       = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList), BindingFlags.Static | BindingFlags.Public);
            JOIN_SELECTOR_METHOD = typeof(ProviderExecutor).GetMethod(nameof(ProviderExecutor.ApplyJoinSelector), BindingFlags.Public | BindingFlags.Static);
        }

    }
}
