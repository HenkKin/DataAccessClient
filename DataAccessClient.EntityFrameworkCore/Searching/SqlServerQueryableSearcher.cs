using System.Linq;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.Relational.Extensions;
using DataAccessClient.Searching;

namespace DataAccessClient.EntityFrameworkCore.Relational.Searching
{
    internal class RelationalQueryableSearcher<TEntity> : IQueryableSearcher<TEntity> where TEntity : class
    {
        public async Task<CriteriaResult<TEntity>> ExecuteAsync(IQueryable<TEntity> queryable, Criteria criteria)
        {
            return await queryable.ToCriteriaResultAsync(criteria);
        }
    }
}
