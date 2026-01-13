using System;
using System.Linq;
using System.Linq.Expressions;

namespace DataAccessClient.EntityFrameworkCore.Relational.Extensions
{
    internal static class OrderByExtensions
    {
        internal static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "OrderBy");
        }
        internal static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "OrderByDescending");
        }
        internal static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "ThenBy");
        }
        internal static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "ThenByDescending");
        }
        internal static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string orderByProperty, string methodName)
        {
            string[] properties = orderByProperty.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (var property in properties)
            {
                var propertyInfo = type.GetProperties()
                    .FirstOrDefault(x => x.Name.Equals(property, StringComparison.CurrentCultureIgnoreCase));

                if (propertyInfo == null)
                {
                    throw new ArgumentException($"OrderBy Column {property} does not exist.", property);
                }
                expr = Expression.Property(expr, propertyInfo);
                type = propertyInfo.PropertyType;
            }
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            object result = typeof(Queryable).GetMethods().Single(
                    method => method.Name == methodName
                              && method.IsGenericMethodDefinition
                              && method.GetGenericArguments().Length == 2
                              && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), type)
                .Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }
    }
}
