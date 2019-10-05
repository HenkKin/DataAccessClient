using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DataAccessClient.Searching;
using LinqKit;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Extensions
{
    internal static class QueryableExtensions
    {
        internal static async Task<CriteriaResult<T>> ToCriteriaResultAsync<T>(this IQueryable<T> source, Criteria criteria) where T: class
        {
            if (!string.IsNullOrWhiteSpace(criteria.Search))
            {
                var searchKeywords = criteria.Search.Split(' ');
                var textProperties = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(string))
                    .ToList();

                var whereClause = string.Empty;
                var searchKeywordFilters = new List<string>();
                const string alias = "e";

                foreach (var searchKeyword in searchKeywords)
                {
                    var fieldsPerSearchKeywordFilters = new List<string>();

                    foreach (var property in textProperties)
                    {
                        fieldsPerSearchKeywordFilters.Add($@"{alias}.{property.Name}.Contains(""{searchKeyword}"")");
                    }

                    if (fieldsPerSearchKeywordFilters.Any())
                    {
                        searchKeywordFilters.Add(string.Join(" || ", fieldsPerSearchKeywordFilters));
                    }
                }

                if (searchKeywordFilters.Any())
                {
                    whereClause = $"{alias} => {string.Join(" && ", searchKeywordFilters.Select(f => $"({f})"))}";
                }

                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    var options = ScriptOptions.Default.AddReferences(typeof(T).Assembly);

                    Expression<Func<T, bool>> whereClauseFunc =
                        CSharpScript.EvaluateAsync<Expression<Func<T, bool>>>(whereClause, options).Result;

                    source = source.Where(whereClauseFunc);
                }
            }


            if (criteria.KeyFilters != null && criteria.KeyFilters.Count > 0)
            {
                var keyFiltersPredicate = PredicateBuilder.New<T>();

                foreach (var keyFilter in criteria.KeyFilters)
                {
                    var propertyInfo = typeof(T).GetProperties().First(p => string.Compare(p.Name, keyFilter.Key, StringComparison.InvariantCultureIgnoreCase) == 0);
                    var paramExpr = Expression.Parameter(typeof(T));
                    var propertyAccessExpr = Expression.MakeMemberAccess(paramExpr, propertyInfo);
                    var guidExpr = Expression.Constant(Guid.Parse(keyFilter.Value));
                    var body = Expression.Equal(propertyAccessExpr, guidExpr);
                    var lambda = Expression.Lambda<Func<T, bool>>(body, paramExpr);

                    keyFiltersPredicate = keyFiltersPredicate.And(lambda);
                }

                source = source.Where(keyFiltersPredicate);
            }

            source = source.ApplySorting(criteria);

            source = source.ApplyPaging(criteria, out Task<int> countTask);

            var totalRecordCount = await countTask;

            foreach (var criteriaInclude in criteria.Includes)
            {
                source = source.Include(criteriaInclude.First().ToString().ToUpper() + criteriaInclude.Substring(1));
            }

            var records = await source.ToListAsync();

            if (totalRecordCount == -1)
            {
                totalRecordCount = records.Count;
            }

            return new CriteriaResult<T>
            {
                TotalRecordCount = totalRecordCount,
                Records = records
            };
        }

        private static IQueryable<T> ApplySorting<T>(this IQueryable<T> source, Criteria request)
        {
            if (!string.IsNullOrWhiteSpace(request.OrderBy))
            {
                return request.OrderByDirection == OrderByDirection.Ascending ? source.OrderBy(request.OrderBy) : source.OrderByDescending(request.OrderBy);
            }

            return source;
        }

        private static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, Criteria criteria, out Task<int> countTask)
        {
            if (criteria.Page.HasValue && criteria.PageSize.HasValue)
            {
                countTask = source.CountAsync();

                return source.Skip((criteria.Page.Value - 1) * criteria.PageSize.Value).Take(criteria.PageSize.Value);
            }

            countTask = Task.FromResult(-1);
            return source;
        }
    }
}