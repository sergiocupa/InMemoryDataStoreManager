using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{
    public class MemoryQueryProvider : IQueryProvider
    {
        private readonly object _artifactObj; // IndexedTypeArtifact<T>
        private readonly Type _elementType;

        public MemoryQueryProvider(object artifactObj)
        {
            _artifactObj = artifactObj;
            _elementType = artifactObj.GetType().GetGenericArguments().First();
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var qt = typeof(MemoryQueryable<>).MakeGenericType(_elementType);
            return (IQueryable)Activator.CreateInstance(qt, this, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) 
        {
            return new MemoryQueryable<TElement>(this, expression);
        }


        public object? Execute(Expression expression) => Execute<object?>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            // extract predicate lambda (if any)
            var lambda = ExtractPredicateLambda(expression);
            FilterNode filterNode;
            if (lambda == null)
            {
                // no predicate -> return all items
                filterNode = new FilterGroup(LogicalOp.And, new List<FilterNode>()); // empty - handle as all
            }
            else
            {
                var visitor = new QueryExpressionVisitor();
                filterNode = visitor.ExtractFilters(lambda.Body);
            }

            // call FilterEvaluator with artifact
            // artifact is generic; call generic method via reflection
            var artifactType = _artifactObj.GetType(); // IndexedTypeArtifact<T>
            var method = typeof(MemoryQueryProvider).GetMethod(nameof(ApplyFiltersGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!;
            var gen = method.MakeGenericMethod(artifactType.GetGenericArguments()[0]);
            var result = gen.Invoke(this, new object[] { _artifactObj, filterNode })!;
            return (TResult)result;
        }

        // helper invoked via reflection
        private IEnumerable<T> ApplyFiltersGeneric<T>(DataObjectInfo<T> artifact, FilterNode node) where T : class
        {
            // Special-case: empty AND group -> return all items
            if (node is FilterGroup fg && fg.Children.Count == 0) return artifact.Items;

            var results = FilterEvaluator.ApplyFilters(artifact, node);
            return results;
        }

        // Finds the predicate lambda from expression tree (supports Where calls)
        private static LambdaExpression? ExtractPredicateLambda(Expression expression)
        {
            // If expression is a MethodCallExpression like q.Where(lambda)
            if (expression is MethodCallExpression mcall)
            {
                // Walk down method calls to find Where and its lambda
                // Example chain: Where(...).Select(...).Take(...)
                // We'll search the arguments for a lambda expression or unary wrapping the lambda
                foreach (var arg in mcall.Arguments)
                {
                    var lam = FindLambdaInExpression(arg);
                    if (lam != null) return lam;
                }

                // recursively search the first argument (source) as it might contain the Where
                return ExtractPredicateLambda(mcall.Arguments[0]);
            }

            // If the expression itself is a lambda (unlikely at this point), return it
            if (expression is LambdaExpression le) return le;

            // No predicate found
            return null;
        }

        private static LambdaExpression? FindLambdaInExpression(Expression expr)
        {
            if (expr is UnaryExpression ue && ue.Operand is LambdaExpression lam) return lam;
            if (expr is LambdaExpression lam2) return lam2;
            return null;
        }
    }
}
