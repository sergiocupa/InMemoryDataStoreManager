using System.Collections;
using System.Linq.Expressions;

namespace InMemoryDataStoreManager.Engine
{
    public class MemoryQueryable<T> : IQueryable<T>
    {
        private readonly Expression _expression;
        private readonly IQueryProvider _provider;


        public MemoryQueryable(IQueryProvider provider)
        {
            _provider   = provider;
            _expression = Expression.Constant(this);
        }

        // Use to build root queryable from artifact
        public MemoryQueryable(DataObjectInfo<T> artifact)
        {
            _provider = new MemoryQueryProvider(artifact);
            _expression = Expression.Constant(this);
        }

        // Used by provider to create child queryables
        internal MemoryQueryable(MemoryQueryProvider provider, Expression expression)
        {
            _provider = provider;
            _expression = expression;
        }

        public Type ElementType => typeof(T);
        public Expression Expression => _expression;
        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator() => Provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
