using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EntityCloner.Microsoft.EntityFrameworkCore;

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

        Task<TEntity> FindByIdAsync(params object[] id);

        TEntity StartChangeTrackingById(params object[] id);
        
        TEntity Update(TEntity entity);

        [Obsolete("Use EntityCloner.Microsoft.EntityFrameworkCore nuget package directly.")]
        Task<TEntity> CloneAsync(params object[] id);

        [Obsolete("Use EntityCloner.Microsoft.EntityFrameworkCore nuget package directly.")]
        Task<TEntity> CloneAsync(Func<IClonableQueryable<TEntity>, IClonableQueryable<TEntity>> includeQuery, params object[] id);
    }
}
