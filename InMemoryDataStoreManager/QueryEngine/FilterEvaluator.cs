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
using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{
    public static class FilterEvaluator
    {
        // node: AST root, artifact: indexed artifact
        public static IEnumerable<T> ApplyFilters<T>(ObjecProvider<T> artifact, FilterNode node) where T : class
        {
            return node switch
            {
                FilterCondition cond => ApplyCondition(artifact, cond),
                FilterGroup grp => ApplyGroup(artifact, grp),
                _ => artifact.Items
            };
        }

        private static IEnumerable<T> ApplyGroup<T>(ObjecProvider<T> artifact, FilterGroup group) where T : class
        {
            var sets = group.Children.Select(child => ApplyFilters(artifact, child));
            if (group.Operator == LogicalOp.And)
            {
                return sets.Aggregate((a, b) => a.Intersect(b));
            }
            else
            {
                return sets.Aggregate((a, b) => a.Union(b));
            }
        }

        private static IEnumerable<T> ApplyCondition<T>(ObjecProvider<T> artifact, FilterCondition cond) where T : class
        {
            return artifact.Items.Where(x => Matches(x, cond));
        }

        private static bool Matches<T>(T item, FilterCondition cond)
        {
            var val = cond.Property.GetValue(item);
            // Handle nulls
            if (val == null && cond.Value == null)
            {
                return cond.Operator == ExpressionType.Equal;
            }
            if (val == null || cond.Value == null) return false;

            var comp = Comparer.Default.Compare(val, cond.Value);
            return cond.Operator switch
            {
                ExpressionType.Equal => Equals(val, cond.Value),
                ExpressionType.GreaterThan => comp > 0,
                ExpressionType.GreaterThanOrEqual => comp >= 0,
                ExpressionType.LessThan => comp < 0,
                ExpressionType.LessThanOrEqual => comp <= 0,
                _ => false
            };
        }
    }
}
