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
using System.Linq.Expressions;

namespace InMemoryDataStoreManager.QueryEngine
{
    internal class ProviderApplicator
    {

        internal static void ApplySkip<T>(ref IEnumerable<T> query, QueryParts parts)
        {
            if (parts.Skip.HasValue) query = query.Skip(parts.Skip.Value);
        }

        internal static void ApplyTake<T>(ref IEnumerable<T> query, QueryParts parts)
        {
            if (parts.Take.HasValue) query = query.Take(parts.Take.Value);
        }

        internal static void ApplyOrder<T>(ref IEnumerable<T> query, QueryParts parts)
        {
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

            if (ordered != null) query = ordered;
        }

        internal static IEnumerable<T> ApplyFilter<T>(ObjecProvider<T> artifact, FilterNode node) where T : class
        {
            if (node is FilterCondition cond)
            {
                // Usa índice se existir, senão filtra manual
                return TryUseIndex<T>(artifact, cond) ?? artifact.Items.Where(x => FilterEvaluator.Matches(x, cond));
            }

            if (node is FilterGroup fg)
            {
                List<IEnumerable<T>> indexedSets = new List<IEnumerable<T>>();
                List<FilterCondition> residual = new List<FilterCondition>();

                // separa filtros indexados e não indexados
                foreach (var child in fg.Children)
                {
                    switch (child)
                    {
                        case FilterCondition fc:
                            var idxResult = TryUseIndex<T>(artifact, fc);
                            if (idxResult != null)
                                indexedSets.Add(idxResult);
                            else
                                residual.Add(fc);
                            break;

                        case FilterGroup subgroup:
                            indexedSets.Add(ApplyFilter(artifact, subgroup));
                            break;
                    }
                }

                // Combina todos os conjuntos indexados usando Intersect (AND) ou Union (OR)
                IEnumerable<T> result;
                if (indexedSets.Count > 0)
                {
                    // otimizado: começa com HashSet para AND
                    if (fg.Operator == LogicalOp.And)
                    {
                        var set = new HashSet<T>(indexedSets[0]);
                        for (int i = 1; i < indexedSets.Count; i++)
                        {
                            set.IntersectWith(indexedSets[i]);
                        }
                        result = set;
                    }
                    else // OR
                    {
                        var set = new HashSet<T>(indexedSets[0]);
                        for (int i = 1; i < indexedSets.Count; i++)
                        {
                            set.UnionWith(indexedSets[i]);
                        }
                        result = set;
                    }
                }
                else
                {
                    result = artifact.Items;
                }

                // Aplica filtros não indexados diretamente sobre o resultado
                foreach (var fc in residual)
                {
                    result = result.Where(x => FilterEvaluator.Matches(x, fc));
                }

                return result;
            }

            return artifact.Items;
        }

        private static IEnumerable<T>? TryUseIndex<T>(ObjecProvider<T> artifact, FilterNode filter) where T : class
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

    }
}
