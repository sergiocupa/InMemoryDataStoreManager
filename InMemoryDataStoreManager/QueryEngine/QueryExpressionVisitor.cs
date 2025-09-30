using System.Linq.Expressions;
using System.Reflection;

namespace InMemoryDataStoreManager.Engine
{
  

    public class QueryExpressionVisitor : ExpressionVisitor
    {

        public QueryParts ExtractParts(Expression expr)
        {
            return VisitParts(expr) ?? new QueryParts();
        }

        private QueryParts? VisitParts(Expression expr)
        {
            if (expr == null) return null;

            // 1) MethodCallExpression: Where, OrderBy, Skip, Take, Max, Min etc.
            if (expr is MethodCallExpression m)
            {
                // process source first (argument[0]) to accumulate parts
                var sourcePart = VisitParts(m.Arguments[0]) ?? new QueryParts();

                var method = m.Method.Name;

                switch (method)
                {
                    case "Where":
                        {
                            // predicate is argument[1] (may be quoted)
                            var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            var condition = VisitFilter(lambda.Body);
                            // merge existing filter with new using AND
                            sourcePart.Filter = MergeFilters(sourcePart.Filter, condition);
                            return sourcePart;
                        }

                    case "OrderBy":
                    case "OrderByDescending":
                        {
                            var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            if (lambda.Body is MemberExpression me && me.Member is PropertyInfo pi)
                            {
                                sourcePart.Orders.Add(new OrderSpec(pi, method == "OrderByDescending"));
                                return sourcePart;
                            }
                            throw new NotSupportedException($"OrderBy com expressão não suportada: {lambda.Body}");
                        }

                    case "ThenBy":
                    case "ThenByDescending":
                        {
                            // ThenBy is like OrderBy but keep previous ordering
                            var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            if (lambda.Body is MemberExpression me && me.Member is PropertyInfo pi)
                            {
                                sourcePart.Orders.Add(new OrderSpec(pi, method.EndsWith("Descending")));
                                return sourcePart;
                            }
                            throw new NotSupportedException($"ThenBy com expressão não suportada: {lambda.Body}");
                        }

                    case "Skip":
                        {
                            var constExpr = EvaluateAsConstant(m.Arguments[1]);
                            sourcePart.Skip = Convert.ToInt32(constExpr);
                            return sourcePart;
                        }

                    case "Take":
                        {
                            var constExpr = EvaluateAsConstant(m.Arguments[1]);
                            sourcePart.Take = Convert.ToInt32(constExpr);
                            return sourcePart;
                        }

                    case "Max":
                    case "Min":
                        {
                            // Max(source, selector)
                            var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            if (lambda.Body is MemberExpression me && me.Member is PropertyInfo pi)
                            {
                                sourcePart.IsAggregation = true;
                                sourcePart.AggregationName = method;
                                sourcePart.AggregationProperty = pi;
                                // keep any prior filter
                                return sourcePart;
                            }
                            throw new NotSupportedException($"{method} com expressão não suportada: {lambda.Body}");
                        }
                    case "Join":
                        {
                            var outer = VisitParts(m.Arguments[0]) ?? new QueryParts();

                            // argumentos: outer, inner, outerKeySelector, innerKeySelector, resultSelector
                            outer.InnerSource      = EvaluateAsConstant(m.Arguments[1]) as IQueryable;
                            outer.OuterKeySelector = (LambdaExpression)StripQuotes(m.Arguments[2]);
                            outer.InnerKeySelector = (LambdaExpression)StripQuotes(m.Arguments[3]);
                            outer.ResultSelector   = (LambdaExpression)StripQuotes(m.Arguments[4]);

                            return outer;
                        }
                    case "Select":
                        {
                            sourcePart.ResultSelector = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            Visit(m.Arguments[0]);
                            return sourcePart;
                        }
                    default:
                        // se for outra chamada de método, tente recursivamente na origem
                        return sourcePart;
                }
            }

            // 2) Se for LambdaExpression diretamente (pode ocorrer se você passar lambda.Body) => processa o corpo
            if (expr is LambdaExpression lam) return VisitParts(lam.Body);

            // 3) Se for BinaryExpression, cria FilterNode (reaproveita seu VisitFilter)
            if (expr is BinaryExpression)
            {
                var node = VisitFilter(expr);
                var parts = new QueryParts();
                parts.Filter = node;
                return parts;
            }

            // 4) Se for UnaryExpression, descompacta e processa
            if (expr is UnaryExpression u)
                return VisitParts(u.Operand);

            // 5) Constant/Parameter/Member sem contexto: não há partes (p.ex. OrderBy selector chegará aqui se chamado diretamente)
            return null;
        }

        // Reaproveita seu VisitFilter para transformar comparações binárias em FilterNode
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

                // tratar casos com Convert(...) ao redor
                if (be.Left is UnaryExpression ul && ul.Operand is MemberExpression uml && be.Right is ConstantExpression crc)
                {
                    var property = uml.Member as PropertyInfo;
                    return new FilterCondition(property, be.NodeType, crc.Value);
                }
                if (be.Right is UnaryExpression ur && ur.Operand is MemberExpression umr && be.Left is ConstantExpression clc)
                {
                    var property = umr.Member as PropertyInfo;
                    return new FilterCondition(property, be.NodeType, clc.Value);
                }
            }

            if (expr is UnaryExpression ue)
                return VisitFilter(ue.Operand);

            throw new NotSupportedException($"Expressão não suportada na condição: {expr}");
        }

        private static object? EvaluateAsConstant(Expression expr)
        {
            // tenta avaliar expressão simples (constante ou closure)
            if (expr is ConstantExpression ce) return ce.Value;
            var lambda = Expression.Lambda(expr);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }

        private static FilterNode? MergeFilters(FilterNode? existing, FilterNode newFilter)
        {
            if (existing == null) return newFilter;
            return new FilterGroup(LogicalOp.And, new List<FilterNode> { existing, newFilter });
        }

    }

    public abstract record FilterNode;

    public record FilterGroup(LogicalOp Operator, List<FilterNode> Children) : FilterNode;

    public record FilterCondition(PropertyInfo Property, ExpressionType Operator, object? Value) : FilterNode;

    // Para métodos
    public record FilterWhere(FilterNode Condition) : FilterNode;

    public record FilterOrderBy(PropertyInfo Property, bool Descending) : FilterNode;

    public record FilterTake(int Count) : FilterNode;

    public record FilterSkip(int Count) : FilterNode;

    public record FilterMax(PropertyInfo Property) : FilterNode;

    public record FilterMin(PropertyInfo Property) : FilterNode;

    public enum LogicalOp { And, Or }

    public record OrderSpec(PropertyInfo Property, bool Descending);


    public class QueryParts
    {
        public FilterNode? Filter { get; set; }
        public List<OrderSpec> Orders { get; } = new();
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public bool IsAggregation { get; set; }
        public string? AggregationName { get; set; } // "Max" / "Min"
        public PropertyInfo? AggregationProperty { get; set; }

        // To JOIN
        public LambdaExpression? OuterKeySelector { get; set; }
        public LambdaExpression? InnerKeySelector { get; set; }
        public LambdaExpression? ResultSelector { get; set; }
        public IQueryable? InnerSource { get; set; }
    }

}
