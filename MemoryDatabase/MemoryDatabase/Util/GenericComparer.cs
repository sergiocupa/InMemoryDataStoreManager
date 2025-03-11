using System.Linq.Expressions;


namespace MemoryDatabase.Util
{


    public class IntEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (int)value1 == (int)value2;
        }
    }
    public class IntNotEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (int)value1 != (int)value2;
        }
    }
    public class IntGreaterThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (int)value1 > (int)value2;
        }
    }
    public class IntGreaterThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (int)value1 >= (int)value2;
        }
    }
    public class IntLessThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (int)value1 < (int)value2;
        }
    }
    public class IntLessThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (int)value1 <= (int)value2;
        }
    }


    public class StringEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (string)value1 == (string)value2;
        }
    }
    public class StringNotEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (string)value1 != (string)value2;
        }
    }
    public class StringGreaterThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return string.Compare((string)value1, (string)value2) > 0;
            //return string.Compare((string)value1, (string)value2, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }
    public class StringGreaterThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return ((string)value1 == (string)value2) || (string.Compare((string)value1, (string)value2) > 0);
        }
    }
    public class StringLessThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return string.Compare((string)value1, (string)value2) < 0;
            //return string.Compare((string)value1, (string)value2, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }
    public class StringLessThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return ((string)value1 == (string)value2) || (string.Compare((string)value1, (string)value2) < 0);
        }
    }


    public class DecimalEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (decimal)value1 == (decimal)value2;
        }
    }
    public class DecimalNotEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (decimal)value1 != (decimal)value2;
        }
    }
    public class DecimalGreaterThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (decimal)value1 > (decimal)value2;
        }
    }
    public class DecimalGreaterThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (decimal)value1 >= (decimal)value2;
        }
    }
    public class DecimalLessThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (decimal)value1 < (decimal)value2;
        }
    }
    public class DecimalLessThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (decimal)value1 <= (decimal)value2;
        }
    }


    public class BoolEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (bool)value1 == (bool)value2;
        }
    }
    public class BoolNotEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (bool)value1 != (bool)value2;
        }
    }



    public class DatetimeEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (DateTime)value1 == (DateTime)value2;
        }
    }
    public class DatetimeNotEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (DateTime)value1 != (DateTime)value2;
        }
    }
    public class DatetimeGreaterThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (DateTime)value1 > (DateTime)value2;
        }
    }
    public class DatetimeGreaterThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (DateTime)value1 >= (DateTime)value2;
        }
    }
    public class DatetimeLessThanComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (DateTime)value1 < (DateTime)value2;
        }
    }
    public class DatetimeLessThanOrEqualComparer : IGenericComparer
    {
        public bool Compare(object value1, object value2)
        {
            return (DateTime)value1 <= (DateTime)value2;
        }
    }


    public class ComparerOp
    {

        public IGenericComparer Equal;
        public IGenericComparer NotEqual;
        public IGenericComparer GreaterThan;
        public IGenericComparer GreaterThanOrEqual;
        public IGenericComparer LessThan;
        public IGenericComparer LessThanOrEqual;


        public ComparerOp(Type type)
        {
            Equal              = GenericComparer.Create(type, ExpressionType.Equal);
            NotEqual           = GenericComparer.Create(type, ExpressionType.NotEqual);
            GreaterThan        = GenericComparer.Create(type, ExpressionType.GreaterThan);
            GreaterThanOrEqual = GenericComparer.Create(type, ExpressionType.GreaterThanOrEqual);
            LessThan           = GenericComparer.Create(type, ExpressionType.LessThan);
            LessThanOrEqual    = GenericComparer.Create(type, ExpressionType.LessThanOrEqual);
        }

    }


    public class GenericComparer
    {
        public static IGenericComparer Create(Type _type, ExpressionType expression)
        {
            var underlying = Nullable.GetUnderlyingType(_type);
            var nullable   = (underlying != null);
            var type       = underlying ?? _type;
            var code       = Type.GetTypeCode(type);

            switch (expression)
            {
                case ExpressionType.Equal:
                    {
                        switch (code)
                        {
                            case TypeCode.Int32:    return new IntEqualComparer();
                            case TypeCode.String:   return new StringEqualComparer();
                            case TypeCode.Decimal:  return new DecimalEqualComparer();
                            case TypeCode.DateTime: return new DatetimeEqualComparer();
                            case TypeCode.Boolean:  return new BoolEqualComparer();
                            case TypeCode.Object:   break;
                            case TypeCode.DBNull:   break;
                        }
                    }
                    break;
                case ExpressionType.NotEqual:
                    {
                        switch (code)
                        {
                            case TypeCode.Int32:    return new IntNotEqualComparer();
                            case TypeCode.String:   return new StringNotEqualComparer();
                            case TypeCode.Decimal:  return new DecimalNotEqualComparer();
                            case TypeCode.DateTime: return new DatetimeNotEqualComparer();
                            case TypeCode.Boolean:  return new BoolNotEqualComparer();
                            case TypeCode.Object:   break;
                            case TypeCode.DBNull:   break;
                        }
                    }
                    break;
                case ExpressionType.GreaterThan:
                    {
                        switch (code)
                        {
                            case TypeCode.Int32:    return new IntGreaterThanComparer();
                            case TypeCode.String:   return new StringGreaterThanComparer();
                            case TypeCode.Decimal:  return new DecimalGreaterThanComparer();
                            case TypeCode.DateTime: return new DatetimeGreaterThanComparer();
                            case TypeCode.Boolean:  break;
                            case TypeCode.Object:   break;
                            case TypeCode.DBNull:   break;
                        }
                    }
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    {
                        switch (code)
                        {
                            case TypeCode.Int32:    return new IntGreaterThanOrEqualComparer();
                            case TypeCode.String:   return new StringGreaterThanOrEqualComparer();
                            case TypeCode.Decimal:  return new DecimalGreaterThanOrEqualComparer();
                            case TypeCode.DateTime: return new DatetimeGreaterThanOrEqualComparer();
                            case TypeCode.Boolean:  break;
                            case TypeCode.Object:   break;
                            case TypeCode.DBNull:   break;
                        }
                    }
                    break;
                case ExpressionType.LessThan:
                    {
                        switch (code)
                        {
                            case TypeCode.Int32:    return new IntLessThanComparer();
                            case TypeCode.String:   return new StringLessThanComparer();
                            case TypeCode.Decimal:  return new DecimalLessThanComparer();
                            case TypeCode.DateTime: return new DatetimeLessThanComparer();
                            case TypeCode.Boolean:  break;
                            case TypeCode.Object:   break;
                            case TypeCode.DBNull:   break;
                        }
                    }
                    break;
                case ExpressionType.LessThanOrEqual:
                    {
                        switch (code)
                        {
                            case TypeCode.Int32:    return new IntLessThanOrEqualComparer();
                            case TypeCode.String:   return new StringLessThanOrEqualComparer();
                            case TypeCode.Decimal:  return new DecimalLessThanOrEqualComparer();
                            case TypeCode.DateTime: return new DatetimeLessThanOrEqualComparer();
                            case TypeCode.Boolean:  break;
                            case TypeCode.Object:   break;
                            case TypeCode.DBNull:   break;
                        }
                    }
                    break;
            }
            throw new NotSupportedException();
        }

    }


    public interface IGenericComparer
    {
        bool Compare(object value1, object value2);
    }
}
