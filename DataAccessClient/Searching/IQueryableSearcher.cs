using System.Linq;
using System.Threading.Tasks;

namespace DataAccessClient.Searching
{
    public interface IQueryableSearcher<TEntity> where TEntity : class
    {
        Task<CriteriaResult<TEntity>> ExecuteAsync(IQueryable<TEntity> queryable, Criteria criteria);
    }
}
