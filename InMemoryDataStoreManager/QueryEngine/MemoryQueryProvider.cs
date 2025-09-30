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
                return ExecuteJoin<TResult>(parts);
            }

            return ExecuteSelector<TResult>(parts);
        }

        public TResult ExecuteSelector<TResult>(QueryParts parts)
        {
            var result = ApplyFiltersGeneric_Method.Invoke(this, new object[] { _artifactObj, parts })!;

            var list = default(object);

            if (parts.ResultSelector != null)
            {
                var rtype = parts.ResultSelector.Body.Type;

                if (rtype == ElementType)
                {
                    lock (_LockObj)
                    {
                        list = ToListByElement.Invoke(null, new object[] { result });
                    }
                }
                else
                {
                    list = ExecuteSelector(parts.ResultSelector, (selector) =>
                    {
                        var selectMethod = SelectorMethod.MakeGenericMethod(result.GetType().GetGenericArguments()[0], parts.ResultSelector.Body.Type);
                        var projected = selectMethod.Invoke(null, new object[] { result, selector });
                        return projected;
                    });
                    return (TResult)list;
                }
            }
            else
            {
                lock (_LockObj)
                {
                    list = ToListByElement.Invoke(null, new object[] { result });
                }
            }
            return (TResult)list;
        }

        public TResult ExecuteJoin<TResult>(QueryParts parts)
        {
            var outerItems = ItemsField.GetValue(_artifactObj);

            var outerType = ElementType;
            var innerType = parts.InnerSource.ElementType;
            var rtype = parts.ResultSelector.Body.Type;

            var joinMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "Join" && m.GetParameters().Length == 5)
            .MakeGenericMethod(outerType, innerType, parts.OuterKeySelector.Body.Type, rtype);

            var compiledOuterKey = parts.OuterKeySelector.Compile();
            var compiledInnerKey = parts.InnerKeySelector.Compile();

            var jist = ExecuteSelector(parts.ResultSelector, (selector) =>
            {
                var joined = joinMethod.Invoke(null, new object[] { outerItems, parts.InnerSource, compiledOuterKey, compiledInnerKey, selector });
                return joined;
            });

            return (TResult)jist;
        }

        public object ExecuteSelector(LambdaExpression selector, Func<Delegate, object> execute_before)
        {
            var list_method = ToListMethod.MakeGenericMethod(selector.Body.Type);

            var res = execute_before(selector.Compile());

            var jist = default(object);
            lock (_LockObj)
            {
                jist = list_method.Invoke(null, new object[] { res });
            }

            return jist;
        }


        // ======== APLICA FILTROS / ORDER / SKIP / TAKE ==============
        private IEnumerable<T> ApplyFiltersGeneric<T>(ObjecProvider<T> artifact, QueryParts parts) where T : class
        {
            IEnumerable<T> query;

            // 1️⃣ Se não há filtros, devolve tudo
            if (parts.Filter is FilterGroup fg && fg.Children.Count == 0)
            {
                query = artifact.Items;
            }
            else if (parts.Filter != null)
            {
                // Se o primeiro nó é FilterCondition simples
                if (parts.Filter is FilterCondition fc)
                {
                    // tenta pegar índice
                    query = TryUseIndex(artifact, fc) ?? FilterEvaluator.ApplyFilters(artifact, parts.Filter);
                }
                else
                {
                    // grupo ou mais complexo → ainda usa FilterEvaluator
                    query = FilterEvaluator.ApplyFilters(artifact, parts.Filter);
                }
            }
            else
            {
                query = artifact.Items;
            }

            // Ordenação (multi-level)
            bool firstOrder = true;
            IOrderedEnumerable<T>? ordered = null;
            foreach (var ord in parts.Orders)
            {
                var prop = ord.Property;
                if (firstOrder)
                {
                    ordered = ord.Descending ? query.OrderByDescending(x => prop.GetValue(x)) : query.OrderBy(x => prop.GetValue(x));
                    firstOrder = false;
                }
                else
                {
                    ordered = ord.Descending ? ordered!.ThenByDescending(x => prop.GetValue(x)) : ordered!.ThenBy(x => prop.GetValue(x));
                }
            }
            if (ordered != null)
                query = ordered;

            // Skip
            if (parts.Skip.HasValue)
                query = query.Skip(parts.Skip.Value);

            // Take
            if (parts.Take.HasValue)
                query = query.Take(parts.Take.Value);

            return query;
        }

        private IEnumerable<T>? TryUseIndex<T>(ObjecProvider<T> artifact, FilterNode filter) where T : class
        {
            // Só funciona para filtros simples do tipo Property OP Constant
            if (filter is not FilterCondition cond)
                return null;

            var idx = artifact.GetIndex(cond.Property);
            if (idx == null) return null;

            // Determinar faixa min/max a partir do operador
            object? min = null;
            object? max = null;
            bool includeMin = true, includeMax = true;

            switch (cond.Operator)
            {
                case ExpressionType.GreaterThan:
                    min = cond.Value; includeMin = false; break;
                case ExpressionType.GreaterThanOrEqual:
                    min = cond.Value; includeMin = true; break;
                case ExpressionType.LessThan:
                    max = cond.Value; includeMax = false; break;
                case ExpressionType.LessThanOrEqual:
                    max = cond.Value; includeMax = true; break;
                case ExpressionType.Equal:
                    min = cond.Value; max = cond.Value;
                    includeMin = includeMax = true; break;
                default:
                    return null; // não suportado
            }

            var result = idx.SearchRange(min!, max!, includeMin, includeMax).Cast<T>();
            return result;
        }


        private readonly object _artifactObj;
        private readonly Type ArtifactType;
        private readonly object _LockObj;
        private readonly Type ElementType;
        private readonly Type ArtifactGeneric;
        private readonly Type ListGeneric;
        private readonly MethodInfo ToListMethod;
        private readonly MethodInfo ToListByElement;
        private readonly MethodInfo ApplyFiltersGeneric_Method;
        private readonly MethodInfo JoinMethod;
        private readonly MethodInfo SelectorMethod;
        private readonly FieldInfo ItemsField;
        private readonly QueryExpressionVisitor Visitor;

        public MemoryQueryProvider(object artifactObj)
        {
            _artifactObj = artifactObj;
            ArtifactType = _artifactObj.GetType();
            ElementType = ArtifactType.GetGenericArguments().First();
            ArtifactGeneric = typeof(MemoryQueryable<>).MakeGenericType(ElementType);
            _LockObj = ArtifactType.GetField("Lock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ListGeneric = typeof(List<>).MakeGenericType(ElementType);

            ItemsField = ArtifactType.GetField("Items");

            ToListMethod    = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList), BindingFlags.Static | BindingFlags.Public);
            ToListByElement = ToListMethod!.MakeGenericMethod(ElementType);

            var method = typeof(MemoryQueryProvider).GetMethod(nameof(ApplyFiltersGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!;
            ApplyFiltersGeneric_Method = method.MakeGenericMethod(ElementType);

            SelectorMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == "Select" && m.GetParameters().Length == 2);

            Visitor = new QueryExpressionVisitor();
        }

    }


}

