using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessClient.Searching;

namespace DataAccessClient
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> GetReadOnlyQuery();

        IQueryable<TEntity> GetChangeTrackingQuery();

        void Add(TEntity newEntity);

        void AddRange(IEnumerable<TEntity> newEntities);

        void Remove(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> entities);

        Task<TEntity> FindByIdAsync(object id);
    }
}
