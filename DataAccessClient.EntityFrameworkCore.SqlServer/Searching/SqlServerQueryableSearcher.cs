using System.Linq;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Extensions;
using DataAccessClient.Searching;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Searching
{
    internal class SqlServerQueryableSearcher<TEntity> : IQueryableSearcher<TEntity> where TEntity : class
    {
        public async Task<CriteriaResult<TEntity>> ExecuteAsync(IQueryable<TEntity> queryable, Criteria criteria)
        {
            return await queryable.ToCriteriaResultAsync(criteria);
        }
    }
}
