using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{
  

    public class QueryExpressionVisitor : ExpressionVisitor
    {

        public FilterNode ExtractFilters(Expression expr)
        {
            return VisitFilter(expr);
        }
        private FilterNode VisitFilter(Expression expr)
        {
            if (expr is BinaryExpression be)
            {
                if (be.NodeType == ExpressionType.AndAlso || be.NodeType == ExpressionType.And)
                    return new FilterGroup(LogicalOp.And, new()
                {
                    VisitFilter(be.Left), VisitFilter(be.Right)
                });

                if (be.NodeType == ExpressionType.OrElse || be.NodeType == ExpressionType.Or)
                    return new FilterGroup(LogicalOp.Or, new()
                {
                    VisitFilter(be.Left), VisitFilter(be.Right)
                });

                // Comparação simples (>, <, >=, <=, ==)
                if (be.Left is MemberExpression m && be.Right is ConstantExpression c)
                {
                    var property = m.Member as PropertyInfo;
                    return new FilterCondition(property, be.NodeType, c.Value);
                }
                if (be.Right is MemberExpression mr && be.Left is ConstantExpression cl)
                {
                    var property = mr.Member as PropertyInfo;
                    return new FilterCondition(property, be.NodeType, cl.Value);
                }
            }

            // se não é BinaryExpression, recursão padrão
            if (expr is UnaryExpression u)
                return VisitFilter(u.Operand);

            throw new NotSupportedException($"Expressão não suportada: {expr}");
        }
    }

    public abstract record FilterNode;
    public record FilterGroup(LogicalOp Operator, List<FilterNode> Children) : FilterNode;
    public record FilterCondition(PropertyInfo Property, ExpressionType Operator, object? Value) : FilterNode;
    public enum LogicalOp { And, Or }

}
