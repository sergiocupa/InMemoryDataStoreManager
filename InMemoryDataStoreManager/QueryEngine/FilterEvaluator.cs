using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{
    public static class FilterEvaluator
    {
        // node: AST root, artifact: indexed artifact
        public static IEnumerable<T> ApplyFilters<T>(DataObjectInfo<T> artifact, FilterNode node) where T : class
        {
            return node switch
            {
                FilterCondition cond => ApplyCondition(artifact, cond),
                FilterGroup grp => ApplyGroup(artifact, grp),
                _ => artifact.Items
            };
        }

        private static IEnumerable<T> ApplyGroup<T>(DataObjectInfo<T> artifact, FilterGroup group) where T : class
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

        private static IEnumerable<T> ApplyCondition<T>(DataObjectInfo<T> artifact, FilterCondition cond) where T : class
        {
            var tp = typeof(T);

            // If index exists for cond.Property, use it
            //var idx = artifact.GetIndex<T>(cond.Property);
            //if (idx != null)
            //{
            //    throw new NotImplementedException("Index-based search not implemented yet.");
            //    // return idx.Search(cond).Where(x => Matches(x, cond)); // idx.Search returns candidate objects; filter for safety
            //}

            // fallback: enumerate Items
            return artifact.Items.Where(x => Matches(x, cond));
        }

        private static bool Matches<T>(T item, FilterCondition cond)
        {
            //var prop = typeof(T).GetProperty(cond.Property.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
           // if (prop == null) return false;
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
