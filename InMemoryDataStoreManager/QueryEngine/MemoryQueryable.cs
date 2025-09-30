using System.Collections;
using System.Linq.Expressions;

namespace InMemoryDataStoreManager.Engine
{
    public class MemoryQueryable<T> : IOrderedQueryable<T> 
    {
        private readonly Expression _expression;
        private readonly IQueryProvider _provider;

        public MemoryQueryable(IQueryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = Expression.Constant(this);
        }

        // Use to build root queryable from artifact
        public MemoryQueryable(MemoryQueryProvider provider)
        {
            _provider   = provider;
            _expression = Expression.Constant(this);
        }

        // Used by provider to create child queryables
        internal MemoryQueryable(MemoryQueryProvider provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public Type ElementType => typeof(T);
        public Expression Expression => _expression;
        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator()
        {
            // Provider deve saber executar Expression em IEnumerable<T>
            var result = _provider.Execute<IEnumerable<T>>(_expression);
            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
